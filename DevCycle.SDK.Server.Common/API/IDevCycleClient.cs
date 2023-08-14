using System;

namespace DevCycle.SDK.Server.Common.API
{
    public interface IDevCycleClient : IDisposable
    {
        string Platform();
        IDevCycleApiClient GetApiClient();
    }

}