using System;
using System.Collections.Generic;
using Hangfire.Client;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;

namespace Hangfire.Harness.Processing
{
    public class SkipWhenPreviousJobIsRunningAttribute : JobFilterAttribute, IClientFilter, IApplyStateFilter
    {
        public void OnCreating(CreatingContext context)
        {
            // We can't handle old storages
            if (!(context.Connection is JobStorageConnection connection)) return;

            // We should run this filter only for background jobs based on 
            // recurring ones
            if (!context.Parameters.TryGetValue("RecurringJobId", out var parameter)) return;

            var recurringJobId = parameter as string;

            // RecurringJobId is malformed. This should not happen, but anyway.
            if (String.IsNullOrWhiteSpace(recurringJobId)) return;

            var running = connection.GetValueFromHash($"recurring-job:{recurringJobId}", "Running");
            if ("yes".Equals(running, StringComparison.OrdinalIgnoreCase))
            {
                context.Canceled = true;
            }
        }

        public void OnCreated(CreatedContext filterContext)
        {
        }

        public void OnStateApplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
        {
            if (context.NewState is EnqueuedState)
            {
                ChangeRunningState(context, "yes");
            }
            else if ((context.NewState.IsFinal && !FailedState.StateName.Equals(context.OldStateName, StringComparison.OrdinalIgnoreCase)) || 
                     (context.NewState is FailedState))
            {
                ChangeRunningState(context, "no");
            }
        }

        public void OnStateUnapplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
        {
        }

        private static void ChangeRunningState(ApplyStateContext context, string state)
        {
            // We can't handle old storages
            if (!(context.Connection is JobStorageConnection connection)) return;

            // Obtaining a recurring job identifier
            var recurringJobId = context.GetJobParameter<string>("RecurringJobId", allowStale: true);
            if (String.IsNullOrWhiteSpace(recurringJobId)) return;

            if (context.Storage.HasFeature(JobStorageFeatures.Transaction.AcquireDistributedLock))
            {
                // Acquire a lock in newer storages to avoid race conditions
                ((JobStorageTransaction)context.Transaction).AcquireDistributedLock(
                    $"lock:recurring-job:{recurringJobId}",
                    TimeSpan.FromSeconds(5));
            }

            // Checking whether recurring job exists
            var recurringJob = connection.GetValueFromHash($"recurring-job:{recurringJobId}", "Job");
            if (String.IsNullOrEmpty(recurringJob)) return;

            // Changing the running state
            context.Transaction.SetRangeInHash(
                $"recurring-job:{recurringJobId}",
                new[] { new KeyValuePair<string, string>("Running", state) });
        }
    }
}