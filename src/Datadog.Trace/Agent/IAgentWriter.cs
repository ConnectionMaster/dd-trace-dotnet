using System.Threading.Tasks;

namespace Datadog.Trace.Agent
{
    internal interface IAgentWriter
    {
        void WriteTrace(ISpan[] trace);

        Task<bool> Ping();

        Task FlushTracesAsync();

        Task FlushAndCloseAsync();
    }
}
