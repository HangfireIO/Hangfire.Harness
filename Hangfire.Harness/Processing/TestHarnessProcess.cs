using System;
using System.Net.Http;
using Hangfire.Server;

namespace Hangfire.Harness.Processing
{
    public sealed class TestHarnessProcess : IBackgroundProcess
    {
        private static readonly TimeSpan RecurringJobUpdateInterval = TimeSpan.FromMinutes(15);

        internal static readonly HttpClient UpdownHttpClient = new HttpClient
        {
            BaseAddress = new Uri("https://pulse.updown.io")
        };

        private readonly int _count;
        private readonly TimeSpan _delay;
        private long? _lastRecurringUpdate;

        public TestHarnessProcess(int count, TimeSpan delay)
        {
            _count = count;
            _delay = delay;
        }

        public void Execute(BackgroundProcessContext context)
        {
            var monitoringApi = context.Storage.GetMonitoringApi();

            if (monitoringApi.EnqueuedCount("default") > _count)
            {
                context.Wait(_delay);
                return;
            }

            var client = new BackgroundJobClient(context.Storage)
            {
                RetryAttempts = 15
            };

            for (var i = 0; i < _count; i++)
            {
                context.StoppingToken.ThrowIfCancellationRequested();
                client.Enqueue<IHarnessV1>(x => x.Perform(0));
            }

            if (_lastRecurringUpdate == null ||
                Environment.TickCount > _lastRecurringUpdate.Value + RecurringJobUpdateInterval.TotalMilliseconds)
            {
                _lastRecurringUpdate = Environment.TickCount;

                RecurringJob.AddOrUpdate<IHarnessV1>("Infinite", x => x.Infinite(default), Cron.Never);
                RecurringJob.TriggerJob("Infinite");
            }
        }
    }
}
