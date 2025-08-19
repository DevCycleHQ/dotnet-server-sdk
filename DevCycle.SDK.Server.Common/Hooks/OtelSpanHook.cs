using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using DevCycle.SDK.Server.Common.Model;

namespace DevCycle.SDK.Server.Common.Hooks
{
    public class OtelSpanHook : EvalHook
    {
        private readonly ActivitySource _activitySource;

        public OtelSpanHook(ActivitySource activitySource)
        {
            _activitySource = activitySource;
        }

        public override async Task<HookContext<T>> BeforeAsync<T>(HookContext<T> context, CancellationToken cancellationToken = default)
        {
            var activity = _activitySource.StartActivity($"feature_flag_evaluation.{context.Key}");

            if (activity != null)
            {
                activity.SetTag("feature_flag.key", context.Key);
                activity.SetTag("feature_flag.provider.name", "devcycle");
                activity.SetTag("feature_flag.context.id", context.User.UserId);
                if (context.Metadata != null)
                {
                    activity.SetTag("feature_flag.project", context.Metadata.Project?.Id);
                    activity.SetTag("feature_flag.environment", context.Metadata.Environment?.Id);
                }
            }

            return await Task.FromResult(context);
        }

        public override Task AfterAsync<T>(HookContext<T> context, Variable<T> variableDetails, VariableMetadata variableMetadata, CancellationToken cancellationToken = default)
        {
            var activity = Activity.Current;
            if (activity != null && variableDetails != null)
            {
                // should variant be the value? what if it is json?
                // activity.SetTag("feature_flag.result.variant", variableDetails.Value?.ToString());
                activity.SetTag("feature_flag.set.id", variableMetadata.FeatureId);
                activity.SetTag("feature_flag.url", $"https://app.devcycle.com/r/p/{context.Metadata.Project.Id}/f/{variableMetadata.FeatureId}");


                if (variableDetails.Eval != null)
                {
                    activity.SetTag("feature_flag.result.reason", variableDetails.Eval.Reason);
                    activity.SetTag("feature_flag.result.reason.details", variableDetails.Eval.Details);
                }
            }
            return Task.CompletedTask;
        }

        public override Task ErrorAsync<T>(HookContext<T> context, System.Exception error, CancellationToken cancellationToken = default)
        {
            Activity.Current?.SetTag("feature_flag.error_message", error.Message);
            Activity.Current?.SetTag("error.type", error.GetType());
            return Task.CompletedTask;
        }

        public override Task FinallyAsync<T>(HookContext<T> context, Variable<T> variableDetails, VariableMetadata variableMetadata, CancellationToken cancellationToken = default)
        {
            Activity.Current?.Dispose();
            return Task.CompletedTask;
        }
    }
}
