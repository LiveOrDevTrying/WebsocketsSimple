using System.Collections.Generic;
using System.Net.WebSockets;

namespace WebsocketsSimple.Server.Models
{
    public interface IUserConnections<T>
    {
        T UserId { get; set; }

        ICollection<IConnectionServer> Connections { get; set; }

        IConnectionServer GetConnection(WebSocket websocket);
    }
}