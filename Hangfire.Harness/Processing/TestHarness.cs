﻿using System.Configuration;
using System.Data;
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
            await Task.Yield();
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