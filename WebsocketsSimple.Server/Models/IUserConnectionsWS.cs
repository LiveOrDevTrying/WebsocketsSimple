using PHS.Networking.Server.Models;
using System.Net.WebSockets;

namespace WebsocketsSimple.Server.Models
{
    public interface IUserConnectionsWS<T> : IUserConnections<T, IConnectionWSServer>
    {
        IConnectionWSServer GetConnection(WebSocket websocket);
    }
}