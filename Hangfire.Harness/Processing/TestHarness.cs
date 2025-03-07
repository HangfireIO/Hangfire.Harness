using System;
using System.Configuration;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Dapper;

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
            throw new Exception("");
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

        public async Task Infinite(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                var requestUri = Environment.GetEnvironmentVariable("PULSE_INFINITE_URI");

                if (!String.IsNullOrWhiteSpace(requestUri))
                {
                    var response = await TestHarnessProcess.UpdownHttpClient.GetAsync(requestUri, token);
                    response.EnsureSuccessStatusCode();
                }

                await Task.Delay(TimeSpan.FromHours(1), token);
            }

            token.ThrowIfCancellationRequested();
        }
    }
}