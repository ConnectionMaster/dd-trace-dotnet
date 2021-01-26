namespace Datadog.Trace
{
    /// <summary>
    /// Interface for scope getter and setter access
    /// </summary>
    internal interface IScopeRawAccess
    {
        IScope Active { get; set; }
    }
}
