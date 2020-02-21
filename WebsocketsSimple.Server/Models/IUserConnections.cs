using PHS.Networking.Server.Models;
using System.Collections.Generic;
using System.Net.WebSockets;

namespace WebsocketsSimple.Server.Models
{
    public interface IUserConnections<T> : IUser<T>
    {
        ICollection<IConnectionServer> Connections { get; set; }

        IConnectionServer GetConnection(WebSocket websocket);
    }
}