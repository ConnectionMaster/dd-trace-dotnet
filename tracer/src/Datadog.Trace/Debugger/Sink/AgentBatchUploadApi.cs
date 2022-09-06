// <copyright file="AgentBatchUploadApi.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

#nullable enable
using System;
using System.Threading.Tasks;
using Datadog.Trace.Agent;
using Datadog.Trace.Agent.DiscoveryService;
using Datadog.Trace.Agent.Transports;
using Datadog.Trace.Logging;

namespace Datadog.Trace.Debugger.Sink
{
    internal class AgentBatchUploadApi : IBatchUploadApi
    {
        private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor<AgentBatchUploadApi>();

        private readonly IApiRequestFactory _apiRequestFactory;
        private readonly IDiscoveryService _discoveryService;

        private AgentBatchUploadApi(IApiRequestFactory apiRequestFactory, IDiscoveryService discoveryService)
        {
            _apiRequestFactory = apiRequestFactory;
            _discoveryService = discoveryService;
        }

        public static AgentBatchUploadApi Create(IApiRequestFactory apiRequestFactory, IDiscoveryService discoveryService)
        {
            return new AgentBatchUploadApi(apiRequestFactory, discoveryService);
        }

        public async Task<bool> SendBatchAsync(ArraySegment<byte> snapshots)
        {
            var uri = _apiRequestFactory.GetEndpoint(_discoveryService.DebuggerEndpoint);
            var request = _apiRequestFactory.Create(uri);

            using var response = await request.PostAsync(snapshots, MimeTypes.Json).ConfigureAwait(false);
            if (response.StatusCode is not (>= 200 and <= 299))
            {
                var content = await response.ReadAsStringAsync().ConfigureAwait(false);
                Log.Warning<int, string>("Failed to upload snapshot with status code {StatusCode} and message: {ResponseContent}", response.StatusCode, content);
                return false;
            }

            return true;
        }
    }
}