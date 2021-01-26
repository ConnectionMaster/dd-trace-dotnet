using System;

namespace Datadog.Trace.Abstractions
{
    // keep temporarily for backwards compatibility
    // use Datadog.Tracer.IScope instead
    internal interface IScope : IDisposable
    {
        ISpan Span { get; }
    }
}
