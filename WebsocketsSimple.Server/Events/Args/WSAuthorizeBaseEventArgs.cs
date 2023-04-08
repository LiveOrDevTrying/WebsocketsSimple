using System.Collections.Generic;
using Tcp.NET.Server.Events.Args;
using WebsocketsSimple.Server.Models;

namespace WebsocketsSimple.Server.Events.Args
{
    public class WSAuthorizeBaseEventArgs<Z, A> : AuthorizeBaseEventArgs<Z> 
        where Z : IdentityWSServer<A>
    {
        public string UpgradeData { get; set; }
        public string[] RequestSubprotocols { get; set; }
        public Dictionary<string, string> RequestHeaders { get; set; }
    }
}

