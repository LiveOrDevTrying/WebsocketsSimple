using PHS.Networking.Models;
using System.IO;
using System.Net.Sockets;
using System.Net.WebSockets;

namespace WebsocketsSimple.Core.Models
{
    public class ConnectionWS : IConnection
    {
        public WebSocket Websocket { get; set; }
        public TcpClient Client { get; set; }
        public Stream Stream { get; set; }
    }
}
