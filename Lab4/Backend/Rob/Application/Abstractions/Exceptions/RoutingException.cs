using System;
using System.Collections.Generic;

namespace Application.Abstractions.Exceptions
{
    public class RoutingException : Exception
    {
        public List<string> DebugLogs { get; }
        
        public RoutingException(string message, List<string> logs) : base(message)
        {
            DebugLogs = logs ?? new List<string>();
        }
    }
}
