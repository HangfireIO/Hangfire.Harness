using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
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
                .UseAutofacActivator(builder.Build())
                .UseSqlServerStorage("HangfireStorage", new SqlServerStorageOptions
                {
                    TransactionIsolationLevel = IsolationLevel.ReadCommitted,
                    DashboardJobListLimit = 1000
                });

            yield return new BackgroundJobServer(
                new BackgroundJobServerOptions(),
                JobStorage.Current,
                Enumerable.Repeat(new HarnessProcess(), 2));
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
