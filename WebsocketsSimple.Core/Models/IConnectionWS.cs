using PHS.Networking.Models;
using System.IO;
using System.Net.Sockets;
using System.Net.WebSockets;

namespace WebsocketsSimple.Core.Models
{
    public interface IConnectionWS : IConnection
    {
        WebSocket Websocket { get; set; }
        TcpClient Client { get; set; }
        Stream Stream { get; set; }
    }
}
