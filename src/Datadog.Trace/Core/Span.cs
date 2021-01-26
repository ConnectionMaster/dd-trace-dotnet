namespace Datadog.Trace
{
    /// <summary>
    /// Abstract base type for all spans.
    /// </summary>
    public abstract class Span
    {
        /// <summary>
        /// Gets or sets operation name.
        /// </summary>
        public abstract string OperationName { get; set; }

        /// <summary>
        /// Gets or sets the resource name.
        /// </summary>
        public abstract string ResourceName { get; set; }

        /// <summary>
        /// Gets or sets the type of request this span represents (ex: web, db).
        /// Not to be confused with span kind.
        /// </summary>
        /// <seealso cref="SpanTypes"/>
        public abstract string Type { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this span represents an error
        /// </summary>
        public abstract bool Error { get; set; }

        /// <summary>
        /// Gets or sets the service name.
        /// </summary>
        public abstract string ServiceName { get; set; }

        /// <summary>
        /// Gets the trace's unique identifier.
        /// </summary>
        public abstract ulong TraceId { get; }

        /// <summary>
        /// Gets the span's unique identifier.
        /// </summary>
        public abstract ulong SpanId { get; }
    }
}
