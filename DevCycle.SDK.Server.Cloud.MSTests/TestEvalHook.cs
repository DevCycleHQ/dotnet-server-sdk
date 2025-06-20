using System;
using System.Threading;
using System.Threading.Tasks;
using DevCycle.SDK.Server.Common.Model;

namespace DevCycle.SDK.Server.Cloud.MSTests
{
public class TestEvalHook : EvalHook
        {
            public int BeforeCallCount { get; private set; }
            public int AfterCallCount { get; private set; }
            public int ErrorCallCount { get; private set; }
            public int FinallyCallCount { get; private set; }

            public bool ThrowBefore { get; set; } = false;
            public bool ThrowAfter { get; set; } = false;
            public bool ThrowError { get; set; } = false;
            public bool ThrowFinally { get; set; } = false;

            public override async Task<HookContext<T>> BeforeAsync<T>(HookContext<T> context, CancellationToken cancellationToken = default)
            {
                BeforeCallCount++;
                if (ThrowBefore)
                {
                    throw new Exception("Before hook error");
                }
                return await base.BeforeAsync(context, cancellationToken);
            }

            public override async Task AfterAsync<T>(HookContext<T> context, Variable<T> details, CancellationToken cancellationToken = default)
            {
                AfterCallCount++;
                if (ThrowAfter)
                {
                    throw new Exception("After hook error");
                }
                await base.AfterAsync(context, details, cancellationToken);
            }

            public override async Task ErrorAsync<T>(HookContext<T> context, Exception error, CancellationToken cancellationToken = default)
            {
                ErrorCallCount++;
                if (ThrowError)
                {
                    throw new Exception("Error hook error");
                }
                await base.ErrorAsync(context, error, cancellationToken);
            }

            public override async Task FinallyAsync<T>(HookContext<T> context, Variable<T> evaluationDetails, CancellationToken cancellationToken = default)
            {
                FinallyCallCount++;
                if (ThrowFinally)
                {
                    throw new Exception("Finally hook error");
                }
                await base.FinallyAsync(context, evaluationDetails, cancellationToken);
            }
        }
}