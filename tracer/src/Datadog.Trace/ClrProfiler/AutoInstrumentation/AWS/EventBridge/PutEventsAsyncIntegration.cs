// <copyright file="PutEventsAsyncIntegration.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

#nullable enable

using System;
using System.ComponentModel;
using System.Threading;
using Datadog.Trace.ClrProfiler.CallTarget;
using Datadog.Trace.DuckTyping;
using Datadog.Trace.Propagators;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.AWS.EventBridge;

/// <summary>
/// AWSSDK.EventBridge PutEventsAsync CallTarget instrumentation
/// </summary>
[InstrumentMethod(
    AssemblyName = "AWSSDK.EventBridge",
    TypeName = "Amazon.EventBridge.AmazonEventBridgeClient",
    MethodName = "PutEventsAsync",
    ReturnTypeName = "System.Threading.Tasks.Task`1[Amazon.EventBridge.Model.PutEventsResponse]",
    ParameterTypeNames = ["Amazon.EventBridge.Model.PutEventsRequest", ClrNames.CancellationToken],
    MinimumVersion = "3.3.0",
    MaximumVersion = "4.*.*",
    IntegrationName = AwsEventBridgeCommon.IntegrationName)]
[Browsable(false)]
[EditorBrowsable(EditorBrowsableState.Never)]
public class PutEventsAsyncIntegration
{
    private const string Operation = "PutEvents";
    private const string SpanKind = SpanKinds.Producer;

    /// <summary>
    /// OnMethodBegin callback
    /// </summary>
    /// <typeparam name="TTarget">Type of the target</typeparam>
    /// <typeparam name="TPutEventsRequest">Type of the request object</typeparam>
    /// <param name="instance">Instance value, aka `this` of the instrumented method</param>
    /// <param name="request">The request for the SNS operation</param>
    /// <param name="cancellationToken">CancellationToken value</param>
    /// <returns>CallTarget state value</returns>
    internal static CallTargetState OnMethodBegin<TTarget, TPutEventsRequest>(TTarget instance, TPutEventsRequest request, CancellationToken cancellationToken)
        where TPutEventsRequest : IPutEventsRequest, IDuckType
    {
        if (request.Instance is null)
        {
            return CallTargetState.GetDefault();
        }

        var tracer = Tracer.Instance;
        var scope = AwsEventBridgeCommon.CreateScope(tracer, Operation, SpanKind, out var tags);
        if (tags is not null)
        {
            var busName = AwsEventBridgeCommon.GetBusName(request.Entries.Value);
            if (busName is not null)
            {
                // We use RuleName to stay consistent with other runtimes
                // TODO rename rulename tag to busname across all runtimes
                tags.RuleName = busName;
            }
        }

        var context = new PropagationContext(scope?.Span.Context, Baggage.Current);
        ContextPropagation.InjectContext(tracer, request, context);

        return new CallTargetState(scope);
    }

    internal static TResponse OnAsyncMethodEnd<TTarget, TResponse>(TTarget instance, TResponse response, Exception exception, in CallTargetState state)
    {
        state.Scope.DisposeWithException(exception);
        return response;
    }
}
