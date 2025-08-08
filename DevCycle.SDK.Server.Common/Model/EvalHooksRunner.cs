using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

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

    public class EvalHooksRunner(ILogger logger, List<EvalHook> hooks = null)
    {
        private readonly List<EvalHook> hooks = hooks ?? [];

        public void AddHook(EvalHook hook)
        {
            hooks.Add(hook);
        }

        public void ClearHooks()
        {
            hooks.Clear();
        }

        public List<EvalHook> GetHooks()
        {
            return hooks;
        }

        public async Task<HookContext<T>> RunBeforeAsync<T>(List<EvalHook> hooksList, HookContext<T> context, 
            CancellationToken cancellationToken = default)
        {
            var result = context;
            try
            {
                foreach (var hook in hooksList)
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

        public async Task RunAfterAsync<T>(List<EvalHook> hooksList, HookContext<T> context,
            Variable<T> details, VariableMetadata variableMetadata,
            CancellationToken cancellationToken = default)
        {
            try
            {
                foreach (var hook in hooksList)
                {
                    await hook.AfterAsync(context, details, variableMetadata, cancellationToken);
                }
            } catch (System.Exception e)    
            {
                throw new AfterHookError("Error executing after hook", e);
            }
        }

        public async Task RunErrorAsync<T>(List<EvalHook> hooksList, HookContext<T> context,
            System.Exception error,
            CancellationToken cancellationToken = default)
        {
            try
            {
                foreach (var hook in hooksList)
                {
                    await hook.ErrorAsync(context, error, cancellationToken);
                }
            } catch (System.Exception e)
            {
                logger.LogError(e, "Error executing error hook");
            }
        }

        public async Task RunFinallyAsync<T>(List<EvalHook> hooksList, HookContext<T> context,
            Variable<T> evaluationDetails, VariableMetadata variableMetadata,
            CancellationToken cancellationToken = default)
        {
            try
            {
                foreach (var hook in hooksList)
                {
                    await hook.FinallyAsync(context, evaluationDetails, variableMetadata, cancellationToken);
                }
            } catch (System.Exception e)
            {
                logger.LogError(e, "Error executing finally hook");
            }
        }
    }
}
