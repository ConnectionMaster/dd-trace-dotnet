using System;
using System.Threading.Tasks;

namespace Datadog.Trace.Agent
{
    internal interface IApi
    {
        Task<bool> SendTracesAsync(ISpan[][] traces);
    }
}
