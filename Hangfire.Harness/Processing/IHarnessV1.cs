using System.Threading;
using System.Threading.Tasks;
using Hangfire.Server;

namespace Hangfire.Harness.Processing
{
    public interface IHarnessV1
    {
        Task Perform(int delay);

        [Queue("{0}")]
        Task Perform(string queue);
        Task<int> Maintenance();
        
        bool Infinite(CancellationToken token);
    }
}