using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;

namespace WebsocketsSimple.Server.Models
{
    public class IdentityWS<T> : IIdentityWS<T>
    {
        public T UserId { get; set; }

        public ICollection<IConnectionWSServer> Connections { get; set; }

        public IConnectionWSServer GetConnection(WebSocket websocket)
        {
            return Connections.FirstOrDefault(s => s.Websocket.GetHashCode() == websocket.GetHashCode());
        }
    }
}
