using System.Net.WebSockets;

namespace WebsocketsSimple.Core.Models
{
    public class ConnectionWS : IConnectionWS
    {
        public WebSocket Websocket { get; set; }
    }
}
