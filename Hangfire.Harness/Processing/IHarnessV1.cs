using System.Threading;
using System.Threading.Tasks;

namespace Hangfire.Harness.Processing
{
    public interface IHarnessV1
    {
        Task Perform(int delay);

        [Queue("{0}")]
        Task Perform(string queue);
        Task<int> Maintenance();

        [ProlongExpiration(expirationTimeMinutes: 60 * 24 * 365)]
        bool Infinite(CancellationToken token);
    }
}