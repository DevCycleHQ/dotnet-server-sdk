using System;

using DevCycle.SDK.Server.Common.Model;
using System.Threading.Tasks;
using DevCycle.SDK.Server.Common.Exception;
using Newtonsoft.Json;
using RestSharp.Portable;

namespace DevCycle.SDK.Server.Common.API
{
    public interface IDVCClient : IDisposable
    {
        string Platform();
        IDVCApiClient GetApiClient();
    }

}