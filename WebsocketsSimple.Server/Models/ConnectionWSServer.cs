using System.Collections.Generic;
using WebsocketsSimple.Core.Models;

namespace WebsocketsSimple.Server.Models
{
    public class ConnectionWSServer : ConnectionWS
    {
        public string Channel { get; set; }
        public KeyValuePair<string, string>[] QueryStringParameters { get; set; }
        public bool Disposed { get; set; }
    }
}
