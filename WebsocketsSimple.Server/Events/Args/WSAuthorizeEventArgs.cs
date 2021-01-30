using System;
using WebsocketsSimple.Server.Models;

namespace WebsocketsSimple.Server.Events.Args
{
    public class WSAuthorizeEventArgs : EventArgs
    {
        public IConnectionWSServer Connection { get; set; }
        public string Token { get; set; }
        public string UpgradeData { get; set; }
    }
}

