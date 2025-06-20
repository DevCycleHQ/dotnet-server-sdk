using System.Threading;
using System.Threading.Tasks;

namespace DevCycle.SDK.Server.Common.Model
{
    public abstract class EvalHook
    {
        public virtual async Task<HookContext<T>> BeforeAsync<T>(HookContext<T> context, CancellationToken cancellationToken = default)
        {
            return await Task.FromResult(context);
        }

        public virtual Task AfterAsync<T>(HookContext<T> context,
            Variable<T> details,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public virtual Task ErrorAsync<T>(HookContext<T> context,
            System.Exception error,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public virtual Task FinallyAsync<T>(HookContext<T> context,
            Variable<T> evaluationDetails,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

}