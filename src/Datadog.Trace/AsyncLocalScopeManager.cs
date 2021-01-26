namespace Datadog.Trace
{
    internal class AsyncLocalScopeManager : ScopeManagerBase
    {
        private readonly AsyncLocalCompat<Scope> _activeScope = new();

        public override Scope Active
        {
            get
            {
                return _activeScope.Get();
            }

            protected set
            {
                _activeScope.Set(value);
            }
        }

        public override IScope ActiveScope { get; protected set; }
    }
}
