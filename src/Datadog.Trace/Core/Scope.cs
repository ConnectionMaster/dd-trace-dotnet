namespace Datadog.Trace
{
    /// <summary>
    /// Abstract base type for all scopes.
    /// </summary>
    public abstract class Scope : Abstractions.IScope
    {
        /// <summary>
        /// Gets the span.
        /// </summary>
        public abstract Span Span { get; }

        /// <summary>
        /// Gets the span.
        /// </summary>
        // keep temporarily for backwards compatibility
        Abstractions.ISpan Abstractions.IScope.Span => Span;

        /// <summary>
        /// Closes the current scope and makes its parent scope active
        /// </summary>
        public abstract void Close();

        /// <summary>
        /// Closes the current scope and makes its parent scope active
        /// </summary>
        public abstract void Dispose();
    }
}
