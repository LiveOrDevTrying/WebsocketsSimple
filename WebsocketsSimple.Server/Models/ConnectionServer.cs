using System.Net.WebSockets;

namespace WebsocketsSimple.Server.Models
{
    public struct ConnectionServer : IConnectionServer
    {
        public bool HasBeenPinged { get; set; }
        public WebSocket Websocket { get; set; }
        public string ConnectionId { get; set; }
    }
}
