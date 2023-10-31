using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DevCycle.SDK.Server.Common.Model;

namespace DevCycle.SDK.Server.Common.API
{
    public interface IDevCycleClient : IDisposable
    {
        string Platform();
        IDevCycleApiClient GetApiClient();
        DevCycleProvider GetOpenFeatureProvider();
        Task<Dictionary<string, Feature>> AllFeatures(DevCycleUser user);
        Task<Dictionary<string, ReadOnlyVariable<object>>> AllVariables(DevCycleUser user);
        Task<Variable<T>> Variable<T>(DevCycleUser user, string key, T defaultValue);
        Task<T> VariableValue<T>(DevCycleUser user, string key, T defaultValue);
        Task<DevCycleResponse> Track(DevCycleUser user, DevCycleEvent userEvent);
        
    }

}