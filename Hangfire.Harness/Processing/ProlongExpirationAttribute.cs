using System;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;

namespace Hangfire.Harness.Processing
{
    public class ProlongExpirationAttribute : JobFilterAttribute, IApplyStateFilter
    {
        private readonly int _expirationTimeMinutes;

        public ProlongExpirationAttribute(int expirationTimeMinutes)
        {
            _expirationTimeMinutes = expirationTimeMinutes;
        }

        public void OnStateApplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
        {
            if (context.NewState.IsFinal)
            {
                context.JobExpirationTimeout = TimeSpan.FromMinutes(_expirationTimeMinutes);
            }
        }

        public void OnStateUnapplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
        {
        }
    }
}