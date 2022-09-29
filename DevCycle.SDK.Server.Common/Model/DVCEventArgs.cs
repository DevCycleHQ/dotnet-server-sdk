using System;
using System.Collections.Generic;
using DevCycle.SDK.Server.Common.Exception;

namespace DevCycle.SDK.Server.Common.Model
{
    public class DVCEventArgs : EventArgs
    {
        public bool Success { get; set; }
        public List<DVCException> Errors { get; set; } = new List<DVCException>();
    }
}