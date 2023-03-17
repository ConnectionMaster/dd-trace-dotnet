﻿# Azure Functions Integration

Azure functions operates in one of two ways:

- In-process
- Isolated (out of process)

## In-process Azure Functions integration

In-process Azure functions were the default approach in early versions of the framework. We currently support v3 and v4 in-process Azure functions. In this model, the "host" process loads the customer's app as a class library.

The Azure Functions host is a normal ASP.NET Core app, but we disable the default diagnostic observer so as to not generate invalid spans. Instead, we instrument the `FunctionInvocationMiddleware` in the host, which provides access to the incoming `HttpContext` for HTTP spans. We use this to create an "outer" span, but at this point we dont have any details about the function that will be executed.

We also instrument the `FunctionExecutor`. This provides all the details about the actual function being executed. We want those details to be applied to the "top-level" span, so for HTTP spans we apply the azure function tags to the initial aspnetcore span too. For non-http triggers (e.g. timer triggers), there will only be a single span created (no top-level HTTP span).

## Isolated Azure Functions integration

Isolated functions are the only supported model for Azure Functions going forward. In this model, instead of the customer's app being a class library, its a real ASP.NET Core application. The host `func.exe` starts the customer app as a sub process, and sets up a GRPC channel between the two apps. The `func.exe` host acts as a proxy for requests to the customer's app.

`func.exe` sets up an in-process Azure Function for every function in the customer's app. Each of the functions in `func.exe` are simple calls that proxy the request to the customer app, and then return the response.

When an HTTP request is received by `func.exe`, it runs the in-process function as normal. As part of the in-process function execution, it creates a GRPC message (by serializing the incoming HTTP requests to a GRPC message), and forwards the request over GRPC to the customer app. The customer's app runs the _real_ Azure function, and returns the response back over GRPC, where it is deserialized and turned into an HTTP response.

```mermaid
sequenceDiagram
    actor client
    participant func as func.exe
    participant app as MyApp
    func->>+app: dotnet.exe MyApp.dll
    func->>+app: GRPC GetFunctions()
    app-->>-func: List<Function>
    loop Each request
        client->>+func: GET /some/trigger
        func->>+app: GRPC (HTTP /some/trigger)
        app-->>-func: GRPC (200 OK)
        func-->>-client: 200 OK
    end
```

To correctly flow and parent the state across each request, our integration does the following:

- In-process instrumentation of `FunctionInvocationMiddleware` in `func.exe`.
  - This is the same integration we use for normal in-process functions. We need to keep the span generated here as it represents the "real" latencies seen by customers calling the function. This is only created for HTTP requests.
- In-process instrumentation of `FunctionExecutor` in `func.exe`.
  - This is the same integration point as for in-process functions, but it serves a slightly different purpose. We don't want to create a span here (this function invocation doesn't represent anything useful), but we _do_ want to enrich the top-level HTTP span with more details about the function invocation. 
  - The "function" being executed here is the "fake" in-process function which mirrors the "real" out-of-process function. Consequently, we don't have real type names, and the function name is prefixed with `Functions.`, e.g. `Functions.MyAppTrigger` instead of `MyAppTrigger`
- Instrumentation of `GrpcMessageConversionExtensions.ToRpcHttp()` in `func.exe`.
  - This is a new integration, which hooks into the method that converts the incoming HTTP request into a GRPC message.
  - We instrument this so that we can inject the context of the new HTTP span created in `FunctionInvocationMiddleware`, so that spans created in the customer app are correctly parented.
  - Note that there may _already_ be a distributed context in the incoming HTTP request. This integration _overwrites_ it to ensure correct parentage.
- `Instrumentation` of `FunctionExecutionMiddleware` in the customer's app.
  - This is the actual function execution in the customer's app, so we can retrieve all the pertinent details about the Azure Function here.
  - One complexity is extracting the distributed HTTP context from the incoming message. We have to do some gnarly DuckType diving through the context variables in order to grab these.
  - Note that the span generated by this integration is directly equivalent to the span generated by `FunctionExecutor` for in-process functions. However, as we've crossed a process boundary (unlike in the in-process case), the span here is a top-level span, and therefore will be decorated with all the extra Azure Function tags.

For an HTTP trigger function, the result is something like this:

```mermaid
gantt
    title HTTP span 
    dateFormat  X
    axisFormat %s
    todayMarker off
    section Baseline
    azure-functions.invoke GET /api/trigger : 0, 100
    azure-functions.invoke Http TriggerCaller : 10, 90
```

For a timer trigger, there's a single span

```mermaid
gantt
    title HTTP span 
    dateFormat  X
    axisFormat %s
    todayMarker off
    section Baseline
    azure-functions.invoke Timer TriggerAllTimer : 0, 100
```