using System;
using System.Collections.Generic;
using WebsocketsSimple.Core.Models;

namespace WebsocketsSimple.Server.Models
{
    public class ConnectionWSServer : ConnectionWS
    {
        public bool HasBeenPinged { get; set; }
        public string ConnectionId { get; set; }
        public string SubProtocol { get; set; }
        public string Path { get; set; }
        public KeyValuePair<string, string>[] QueryStringParameters { get; set; }
        public DateTime NextPingTime { get; set; }
    }
}
