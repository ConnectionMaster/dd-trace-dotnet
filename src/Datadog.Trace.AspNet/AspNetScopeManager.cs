using System;
using System.Web;

namespace Datadog.Trace.AspNet
{
    internal class AspNetScopeManager : ScopeManagerBase
    {
        private readonly string _name = "__Datadog_Scope_Current__" + Guid.NewGuid();
        private readonly AsyncLocalCompat<IScope> _activeScopeFallback = new AsyncLocalCompat<IScope>();

        public override IScope Active
        {
            get
            {
                var activeScope = _activeScopeFallback.Get();
                if (activeScope != null)
                {
                    return activeScope;
                }

                return HttpContext.Current?.Items[_name] as Scope;
            }

            protected set
            {
                _activeScopeFallback.Set(value);

                var httpContext = HttpContext.Current;
                if (httpContext != null)
                {
                    httpContext.Items[_name] = value;
                }
            }
        }
    }
}
