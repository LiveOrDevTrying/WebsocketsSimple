using PHS.Networking.Server.Models;
using System.Net.WebSockets;

namespace WebsocketsSimple.Server.Models
{
    public interface IIdentityWS<T> : IIdentity<T, IConnectionWSServer>
    {
        IConnectionWSServer GetConnection(WebSocket websocket);
    }
}