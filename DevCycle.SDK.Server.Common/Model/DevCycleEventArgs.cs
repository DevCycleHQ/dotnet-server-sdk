using System;
using System.Collections.Generic;
using DevCycle.SDK.Server.Common.Exception;

namespace DevCycle.SDK.Server.Common.Model
{
    public class DevCycleEventArgs : EventArgs
    {
        public bool Success { get; set; }
        public List<DevCycleException> Errors { get; set; } = new List<DevCycleException>();
    }
}