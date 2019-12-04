using WebsocketsSimple.Server.Enums;
using System;
using WebsocketsSimple.Core.Events.Args;
using WebsocketsSimple.Core.Models;

namespace WebsocketsSimple.Server.Events
{
    public class WSConnectionAuthEventArgs : WSConnectionEventArgs
    {
        public Guid UserId { get; set; }
        public ConnectionWebsocketDTO Connection { get; set; }
        public BLLConnectionEventType BLLConnectionEventType { get; set; }
    }
}
