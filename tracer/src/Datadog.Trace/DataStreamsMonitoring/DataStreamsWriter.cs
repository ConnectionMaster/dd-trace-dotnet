﻿// <copyright file="DataStreamsWriter.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

#nullable enable
using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using Datadog.Trace.Agent.DiscoveryService;
using Datadog.Trace.Configuration;
using Datadog.Trace.DataStreamsMonitoring.Aggregation;
using Datadog.Trace.DataStreamsMonitoring.Transport;
using Datadog.Trace.Logging;
using Datadog.Trace.Util;

namespace Datadog.Trace.DataStreamsMonitoring;

internal class DataStreamsWriter : IDataStreamsWriter
{
    private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor<DataStreamsWriter>();

    private readonly object _initLock = new();
    private readonly long _bucketDurationMs;
    private readonly BoundedConcurrentQueue<StatsPoint> _buffer = new(queueLimit: 10_000);
    private readonly BoundedConcurrentQueue<BacklogPoint> _backlogBuffer = new(queueLimit: 10_000);
    private readonly TimeSpan _waitTimeSpan = TimeSpan.FromMilliseconds(10);
    private readonly DataStreamsAggregator _aggregator;
    private readonly IDiscoveryService _discoveryService;
    private readonly IDataStreamsApi _api;
    private readonly bool _isInDefaultState;

    private readonly TaskCompletionSource<bool> _processExit = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private MemoryStream? _serializationBuffer;
    private long _pointsDropped;
    private int _flushRequested;
    private Task? _processTask;
    private Timer? _flushTimer;

    private int _isSupported = SupportState.Unknown;
    private bool _isInitialized;

    public DataStreamsWriter(
        TracerSettings settings,
        DataStreamsAggregator aggregator,
        IDataStreamsApi api,
        long bucketDurationMs,
        IDiscoveryService discoveryService)
    {
        _isInDefaultState = settings.IsDataStreamsMonitoringInDefaultState;
        _aggregator = aggregator;
        _api = api;
        _discoveryService = discoveryService;
        _discoveryService.SubscribeToChanges(HandleConfigUpdate);
        _bucketDurationMs = bucketDurationMs;
    }

    /// <summary>
    /// Public for testing only
    /// </summary>
    public event EventHandler<EventArgs>? FlushComplete;

    /// <summary>
    /// Gets the number of points dropped due to a full buffer or disabled DSM.
    /// Public for testing only
    /// </summary>
    public long PointsDropped => Interlocked.Read(ref _pointsDropped);

    public static DataStreamsWriter Create(
        TracerSettings settings,
        IDiscoveryService discoveryService,
        string defaultServiceName)
        => new(
            settings,
            new DataStreamsAggregator(
                new DataStreamsMessagePackFormatter(settings, defaultServiceName),
                bucketDurationMs: DataStreamsConstants.DefaultBucketDurationMs),
            new DataStreamsApi(DataStreamsTransportStrategy.GetAgentIntakeFactory(settings.Exporter)),
            bucketDurationMs: DataStreamsConstants.DefaultBucketDurationMs,
            discoveryService);

    private void Initialize()
    {
        lock (_initLock)
        {
            if (_processTask != null)
            {
                return;
            }

            _processTask = Task.Run(ProcessQueueLoopAsync);
            _processTask.ContinueWith(t => Log.Error(t.Exception, "Error in processing task"), TaskContinuationOptions.OnlyOnFaulted);
            _flushTimer = new Timer(
                x => ((DataStreamsWriter)x!).RequestFlush(),
                this,
                dueTime: _bucketDurationMs,
                period: _bucketDurationMs);

            Volatile.Write(ref _isInitialized, true);
        }
    }

    public void Add(in StatsPoint point)
    {
        if (Volatile.Read(ref _isSupported) != SupportState.Unsupported)
        {
            if (!Volatile.Read(ref _isInitialized))
            {
                Initialize();
            }

            if (_buffer.TryEnqueue(point))
            {
                return;
            }
        }

        Interlocked.Increment(ref _pointsDropped);
    }

    public void AddBacklog(in BacklogPoint point)
    {
        if (!Volatile.Read(ref _isInitialized))
        {
            Initialize();
        }

        if (Volatile.Read(ref _isSupported) != SupportState.Unsupported)
        {
            if (_backlogBuffer.TryEnqueue(point))
            {
                return;
            }
        }

        Interlocked.Increment(ref _pointsDropped);
    }

    public async Task DisposeAsync()
    {
        _discoveryService.RemoveSubscription(HandleConfigUpdate);
#if NETCOREAPP3_1_OR_GREATER
        if (_flushTimer != null)
        {
            await _flushTimer.DisposeAsync().ConfigureAwait(false);
        }
#else
        _flushTimer?.Dispose();
#endif
        await FlushAndCloseAsync().ConfigureAwait(false);
    }

    private async Task FlushAndCloseAsync()
    {
        if (!_processExit.TrySetResult(true))
        {
            return;
        }

        // nothing else to do, since the writer was not fully initialized
        if (!Volatile.Read(ref _isInitialized) || _processTask == null)
        {
            return;
        }

        // request a final flush - as the _processExit flag is now set
        // this ensures we will definitely flush all the stats
        // (and sets the mutex if it isn't already set)
        RequestFlush();

        // wait for the processing loop to complete
        var completedTask = await Task.WhenAny(
                                           _processTask,
                                           Task.Delay(TimeSpan.FromSeconds(20)))
                                      .ConfigureAwait(false);

        if (completedTask != _processTask)
        {
            Log.Error("Could not flush all data streams stats before process exit");
        }
    }

    private void RequestFlush()
    {
        Interlocked.Exchange(ref _flushRequested, 1);
    }

    private async Task WriteToApiAsync()
    {
        // This method blocks ingestion of new stats points into the aggregator,
        // but they will continue to be added to the queue, and will be processed later
        // Default buffer capacity matches Java implementation:
        // https://cs.github.com/DataDog/dd-trace-java/blob/3386bd137e58ed7450d1704e269d3567aeadf4c0/dd-trace-core/src/main/java/datadog/trace/core/datastreams/MsgPackDatastreamsPayloadWriter.java?q=MsgPackDatastreamsPayloadWriter#L28
        _serializationBuffer ??= new MemoryStream(capacity: 512 * 1024);

        var flushTimeNs = _processExit.Task.IsCompleted
                              ? long.MaxValue // flush all buckets
                              : DateTimeOffset.UtcNow.ToUnixTimeNanoseconds(); // don't flush current bucket

        bool wasDataWritten;
        _serializationBuffer.SetLength(0); // reset the stream
        using (var gzip = new GZipStream(_serializationBuffer, CompressionLevel.Fastest, leaveOpen: true))
        {
            wasDataWritten = _aggregator.Serialize(gzip, flushTimeNs);
        }

        if (wasDataWritten && (Volatile.Read(ref _isSupported) == SupportState.Supported))
        {
            // This flushes on the same thread as the processing loop
            var data = new ArraySegment<byte>(_serializationBuffer.GetBuffer(), offset: 0, (int)_serializationBuffer.Length);

            var success = await _api.SendAsync(data).ConfigureAwait(false);

            var dropCount = Interlocked.Exchange(ref _pointsDropped, 0);
            if (success)
            {
                Log.Debug("Flushed {Count}bytes to data streams intake. {Dropped} points were dropped since last flush", data.Count, dropCount);
            }
            else
            {
                Log.Warning("Error flushing {Count}bytes to data streams intake. {Dropped} points were dropped since last flush", data.Count, dropCount);
            }
        }
    }

    private async Task ProcessQueueLoopAsync()
    {
        var isFinalFlush = false;
        while (true)
        {
            try
            {
                while (_buffer.TryDequeue(out var statsPoint))
                {
                    _aggregator.Add(in statsPoint);
                }

                while (_backlogBuffer.TryDequeue(out var backlogPoint))
                {
                    _aggregator.AddBacklog(in backlogPoint);
                }

                var flushRequested = Interlocked.CompareExchange(ref _flushRequested, 0, 1);
                if (flushRequested == 1)
                {
                    await WriteToApiAsync().ConfigureAwait(false);
                    FlushComplete?.Invoke(this, EventArgs.Empty);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occured in the processing thread");
            }

            if (_processExit.Task.IsCompleted)
            {
                if (isFinalFlush)
                {
                    return;
                }

                // do one more loop to make sure everything is flushed
                RequestFlush();
                isFinalFlush = true;
                continue;
            }

            // The logic is copied from https://github.com/dotnet/runtime/blob/main/src/libraries/Common/tests/System/Threading/Tasks/TaskTimeoutExtensions.cs#L26
            // and modified to avoid dealing with exceptions
            var tcs = new TaskCompletionSource<bool>();
            using (new Timer(s => ((TaskCompletionSource<bool>)s!).SetResult(true), tcs, _waitTimeSpan, Timeout.InfiniteTimeSpan))
            {
                await Task.WhenAny(_processExit.Task, tcs.Task).ConfigureAwait(false);
            }
        }
    }

    private void HandleConfigUpdate(AgentConfiguration config)
    {
        var isSupported = string.IsNullOrEmpty(config.DataStreamsMonitoringEndpoint)
                              ? SupportState.Unsupported
                              : SupportState.Supported;
        var wasSupported = Volatile.Read(ref _isSupported);

        if (isSupported != wasSupported)
        {
            _isSupported = isSupported;
            if (isSupported == SupportState.Supported)
            {
                Log.Information("Data streams monitoring supported, enabling flush");
            }
            else
            {
                const string msg = "Data streams monitoring was enabled but is not supported by the Agent. Disabling Data streams. " +
                          "Consider upgrading your Datadog Agent to at least version 7.34.0+";
                if (_isInDefaultState)
                {
                    Log.Information(msg);
                }
                else
                {
                    Log.Warning(msg);
                }
            }
        }
    }

    private static class SupportState
    {
        public const int Unknown = 0;
        public const int Supported = 1;
        public const int Unsupported = 2;
    }
}
