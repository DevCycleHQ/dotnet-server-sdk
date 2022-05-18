using System;
using DevCycle.SDK.Server.Common.Exception;

namespace DevCycle.SDK.Server.Common.Model
{
    public class DVCEventArgs : EventArgs
    {
        public bool Success { get; set; }
        public DVCException Error { get; set; }
    }
}