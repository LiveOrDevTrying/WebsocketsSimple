using PHS.Networking.Models;
using System.Net.WebSockets;

namespace WebsocketsSimple.Core.Models
{
    public interface IConnectionWS : IConnection
    {
        WebSocket Websocket { get; set; }
    }
}
