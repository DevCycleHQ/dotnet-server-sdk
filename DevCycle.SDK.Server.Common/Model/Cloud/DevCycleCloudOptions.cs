using System.Collections.Generic;

namespace DevCycle.SDK.Server.Common.Model.Cloud
{
    public class DevCycleCloudOptions: IDevCycleOptions
    {
        public bool EnableEdgeDB { get; private set; }
        
        public List<EvalHook> EvalHooks { get; private set; }
        

        public DevCycleCloudOptions(bool enableEdgeDB = false, List<EvalHook> evalHooks = null)
        {
            EnableEdgeDB = enableEdgeDB;
            EvalHooks = evalHooks;
        }
    }
}