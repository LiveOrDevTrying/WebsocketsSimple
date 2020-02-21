using System.Net.WebSockets;

namespace WebsocketsSimple.Core.Models
{
    public interface IConnection
    {
        WebSocket Websocket { get; set; }
    }
}
