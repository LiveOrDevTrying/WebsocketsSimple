using PHS.Core.Events.Args.NetworkEventArgs;
using PHS.Core.Models;
using System.Net.WebSockets;

namespace WebsocketsSimple.Core.Events.Args
{
    public class WSMessageEventArgs : MessageEventArgs
    {
        public WebSocket Websocket { get; set; }
        public PacketDTO Packet { get; set; }
    }
}
