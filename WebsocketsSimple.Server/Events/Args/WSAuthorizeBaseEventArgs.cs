using PHS.Core.Events.Args;
using System.Collections.Generic;
using WebsocketsSimple.Server.Models;

namespace WebsocketsSimple.Server.Events.Args
{
    public class WSAuthorizeBaseEventArgs<Z, A> : BaseArgs 
        where Z : IdentityWSServer<A>
    {
        public byte[] Token { get; set; }
        public Z Connection { get; set; }
        public string UpgradeData { get; set; }
        public string[] RequestSubprotocols { get; set; }
        public Dictionary<string, string> RequestHeaders { get; set; }
    }
}

