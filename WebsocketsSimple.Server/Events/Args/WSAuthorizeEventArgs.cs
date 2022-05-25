using System;
using WebsocketsSimple.Server.Models;

namespace WebsocketsSimple.Server.Events.Args
{
    public class WSAuthorizeEventArgs<T> : EventArgs
    {
        public IdentityWSServer<T> Connection { get; set; }
        public string UpgradeData { get; set; }
        public string[] RequestedSubprotocols { get; set; }
    }
}

