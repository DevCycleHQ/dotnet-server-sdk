using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DevCycle.SDK.Server.Common.Model
{
    public class BeforeHookError : System.Exception
    {
        public BeforeHookError(string message, System.Exception e) : base(message) { }
    }

    public class AfterHookError : System.Exception
    {
        public AfterHookError(string message, System.Exception e) : base(message) { }
    }

    public class EvalHooksRunner
    {
        private readonly List<EvalHook> hooks;

        public EvalHooksRunner()
        {   
            hooks = new List<EvalHook>();
        }

        public void AddHook(EvalHook hook)
        {
            hooks.Add(hook);
        }

        public void ClearHooks()
        {
            hooks.Clear();
        }

        public async Task<HookContext<T>> RunBeforeAsync<T>(HookContext<T> context, 
            CancellationToken cancellationToken = default)
        {
            HookContext<T> result = context;
            try
            {
                foreach (var hook in hooks)
                {
                    result = await hook.BeforeAsync(result, cancellationToken);
                }
                return result;
            }
            catch (System.Exception e)
            {
                throw new BeforeHookError("Error executing before hook", e);
            }
        }

        public async Task RunAfterAsync<T>(HookContext<T> context,
            Variable<T> details,
            CancellationToken cancellationToken = default)
        {
            try
            {
                foreach (var hook in hooks)
                {
                    await hook.AfterAsync(context, details, cancellationToken);
                }
            } catch (System.Exception e)    
            {
                throw new AfterHookError("Error executing after hook", e);
            }
        }

        public async Task RunErrorAsync<T>(HookContext<T> context,
            System.Exception error,
            CancellationToken cancellationToken = default)
        {
            foreach (var hook in hooks)
            {
                try
                {
                    await hook.ErrorAsync(context, error, cancellationToken);
                }
                catch (System.Exception e)
                {
                    // logger.LogError(e, "Error executing error hook");
                }
            }
        }

        public async Task RunFinallyAsync<T>(HookContext<T> context,
            Variable<T> evaluationDetails,
            CancellationToken cancellationToken = default)
        {
            foreach (var hook in hooks)
            {
                try
                {
                    await hook.FinallyAsync(context, evaluationDetails, cancellationToken);
                }
                catch (System.Exception e)
                {
                    // logger.LogError(e, "Error executing finally hook");
                }
            }
        }
    }
}
