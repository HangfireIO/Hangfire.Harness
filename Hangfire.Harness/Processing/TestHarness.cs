using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Dapper;
using Hangfire.Server;

namespace Hangfire.Harness.Processing
{
    public class TestHarness : IHarnessV1
    {
        public Task Perform(int delay)
        {
            return Task.CompletedTask;
        }

        public async Task Perform(string queue)
        {
            await Task.Yield();
        }

        public Task<bool> FeedJobs(PerformContext context, int count)
        {
            var monitoringApi = context.Storage.GetMonitoringApi();

            if (monitoringApi.EnqueuedCount("default") < count)
            {
                var client = new BackgroundJobClient(context.Storage)
                {
                    RetryAttempts = 15
                };

                for (var i = 0; i < count; i++)
                {
                    context.CancellationToken.ShutdownToken.ThrowIfCancellationRequested();
                    client.Enqueue<IHarnessV1>(x => x.Perform(0));
                }

                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        public async Task<int> Maintenance()
        {
            using (var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["HangfireStorage"].ConnectionString))
            {
                return await connection.ExecuteAsync("AzureSQLMaintenance", new
                {
                    operation = "index",
                    mode = "smart"
                }, commandType: CommandType.StoredProcedure, commandTimeout: 0);
            }
        }
    }
}