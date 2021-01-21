using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;

namespace WebsocketsSimple.Server.Models
{
    public class UserConnections<T> : IUserConnections<T>
    {
        public T UserId { get; set; }

        public ICollection<IConnectionServer> Connections { get; set; }

        public IConnectionServer GetConnection(WebSocket websocket)
        {
            return Connections.FirstOrDefault(s => s.Websocket.GetHashCode() == websocket.GetHashCode());
        }
    }
}
