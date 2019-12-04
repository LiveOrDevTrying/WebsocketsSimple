using System;
using WebsocketsSimple.Core.Models;
using WebsocketsSimple.Core.Events.Args;

namespace WebsocketsSimple.Server.Events
{
    public class WSMessageAuthEventArgs : WSMessageEventArgs
    {
        public ConnectionWebsocketDTO Connection { get; set; }
        public Guid UserId { get; set; }
    }
}
