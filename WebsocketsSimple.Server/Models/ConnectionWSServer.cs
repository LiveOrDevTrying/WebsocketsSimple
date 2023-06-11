using PHS.Networking.Server.Models;
using System.Collections.Generic;
using WebsocketsSimple.Core.Models;

namespace WebsocketsSimple.Server.Models
{
    public class ConnectionWSServer : ConnectionWS, IConnectionServer
    {
        public string Channel { get; set; }
        public KeyValuePair<string, string>[] QueryStringParameters { get; set; }
    }
}
