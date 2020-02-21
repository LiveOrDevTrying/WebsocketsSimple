using PHS.Networking.Server.Models;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;

namespace WebsocketsSimple.Server.Models
{
    public class UserConnections<T> : User<T>, IUserConnections<T>
    {
        public ICollection<IConnectionServer> Connections { get; set; }

        public IConnectionServer GetConnection(WebSocket websocket)
        {
            return Connections.FirstOrDefault(s => s.Websocket.GetHashCode() == websocket.GetHashCode());
        }
    }
}
