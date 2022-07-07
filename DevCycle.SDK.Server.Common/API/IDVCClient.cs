using System;

namespace DevCycle.SDK.Server.Common.API
{
    public interface IDVCClient : IDisposable
    {
        string Platform();
        IDVCApiClient GetApiClient();
    }

}