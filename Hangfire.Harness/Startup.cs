﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using Autofac;
using Hangfire.Dashboard;
using Hangfire.Harness;
using Hangfire.Harness.Processing;
using Hangfire.SqlServer;
using Microsoft.Owin;
using Owin;


[assembly: OwinStartup(typeof(Startup))]
namespace Hangfire.Harness
{
    public class Startup
    {
        private static IEnumerable<IDisposable> ConfigureHangfire()
        {
            var builder = new ContainerBuilder();
            builder.Register(x => new TestHarness()).As<IHarnessV1>();

            GlobalConfiguration.Configuration
                .UseIgnoredAssemblyVersionTypeResolver()
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseRecommendedSerializerSettings()
                .UseAutofacActivator(builder.Build())
                .UseSimpleAssemblyNameTypeSerializer()
                .UseSerilogLogProvider();

            if (!String.IsNullOrEmpty(ConfigurationManager.AppSettings["RedisStorage"]))
            {
                GlobalConfiguration.Configuration
                    .UseRedisStorage(ConfigurationManager.AppSettings["RedisStorage"])
                    .WithJobExpirationTimeout(TimeSpan.FromHours(1))
                    .UseRedisMetrics();
            }
            else
            {
                GlobalConfiguration.Configuration
                    .UseSqlServerStorage("HangfireStorage", new SqlServerStorageOptions
                    {
                        DashboardJobListLimit = 1000,
                        EnableHeavyMigrations = true,
                        InactiveStateExpirationTimeout = TimeSpan.FromDays(7)
                    })
                    .WithJobExpirationTimeout(TimeSpan.FromHours(1));

                RecurringJob.AddOrUpdate<IHarnessV1>("IHarnessV1.Maintenance", x => x.Maintenance(), Cron.Daily(01, 00));
            }

            RecurringJob.AddOrUpdate<IHarnessV1>("Infinite", x => x.Infinite(default), Cron.Never);

            yield return new BackgroundJobServer(
                new BackgroundJobServerOptions
                {
                    SchedulePollingInterval = TimeSpan.FromSeconds(1),
                    TaskScheduler = null,
                },
                JobStorage.Current,
                new[] { new TestHarnessProcess(3000, TimeSpan.FromSeconds(5)) });
        }

        public void Configuration(IAppBuilder app)
        {
            app.UseHangfireAspNet(ConfigureHangfire);
            app.UseHangfireDashboard("/hangfire", new DashboardOptions
            {
                Authorization = new IDashboardAuthorizationFilter[0]
            });
        }
    }
}
