using System;
using Hangfire.Server;

namespace Hangfire.Harness.Processing
{
    public sealed class TestHarnessProcess : IBackgroundProcess
    {
        private readonly int _count;
        private readonly TimeSpan _delay;

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
        }
    }
}
