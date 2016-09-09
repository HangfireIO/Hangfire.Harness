using System;
using Hangfire.Server;

namespace Hangfire.Harness.Processing
{
    public class HarnessProcess : IBackgroundProcess
    {
        public void Execute(BackgroundProcessContext context)
        {
            var client = new BackgroundJobClient(context.Storage);
            client.Enqueue<IHarnessV1>(x => x.Perform(0));
        }
    }
}