using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Hangfire.States;
using Microsoft.Data.SqlClient;

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
            var inconsistentJobIds = new List<long>();
            
            using (var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["HangfireStorage"].ConnectionString))
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
SET LOCK_TIMEOUT 1000;
SELECT j.[Id] FROM [HangFire].[Job] j
LEFT JOIN [HangFire].[JobQueue] jq ON j.[Id] = jq.[JobId]
WHERE j.[StateName] = N'Enqueued' AND jq.[Id] IS NULL";
                command.CommandTimeout = 1_000;

                await connection.OpenAsync();

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (reader.Read())
                    {
                        var jobId = reader.GetInt64(reader.GetOrdinal("Id"));
                        inconsistentJobIds.Add(jobId);
                    }
                }
            }

            foreach (var inconsistentJobId in inconsistentJobIds)
            {
                BackgroundJob.Requeue(
                    inconsistentJobId.ToString(CultureInfo.InvariantCulture),
                    EnqueuedState.StateName);
            }

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