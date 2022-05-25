using System;
using System.Collections.Generic;
using WebsocketsSimple.Core.Models;

namespace WebsocketsSimple.Server.Models
{
    public class ConnectionWSServer : ConnectionWS
    {
        public int PingAttempts { get; set; }
        public string ConnectionId { get; set; }
        public string Path { get; set; }
        public KeyValuePair<string, string>[] QueryStringParameters { get; set; }
    }
}
