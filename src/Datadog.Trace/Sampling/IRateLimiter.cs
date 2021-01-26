namespace Datadog.Trace.Sampling
{
    internal interface IRateLimiter
    {
        bool Allowed(ISpan span);

        // keep temporarily for backwards compatibility
        bool Allowed(Span span);

        float GetEffectiveRate();
    }
}
