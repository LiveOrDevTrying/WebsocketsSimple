using System.Net.WebSockets;

namespace WebsocketsSimple.Core.Models
{
    public class ConnectionWebsocketDTO
    {
        public WebSocket Websocket { get; set; }
        public bool HasBeenPinged { get; set; }
    }
}
