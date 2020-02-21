using System.Net.WebSockets;

namespace WebsocketsSimple.Core.Models
{
    public struct Connection : IConnection
    {
        public WebSocket Websocket { get; set; }
    }
}
