using PHS.Networking.Models;
using System.Net.Sockets;
using System.Net.WebSockets;

namespace WebsocketsSimple.Core.Models
{
    public class ConnectionWS : IConnection
    {
        public WebSocket Websocket { get; set; }
        public TcpClient TcpClient { get; set; }
    }
}
