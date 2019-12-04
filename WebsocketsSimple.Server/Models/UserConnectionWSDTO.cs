using PHS.Core.Models;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using WebsocketsSimple.Core.Models;

namespace WebsocketsSimple.Server.Models
{
    public class UserConnectionWSDTO : UserConnectionDTO, IUserConnectionWSDTO
    {
        public ICollection<ConnectionWebsocketDTO> Connections { get; set; }

        public ConnectionWebsocketDTO GetConnection(WebSocket websocket)
        {
            return Connections.FirstOrDefault(s => s.Websocket.GetHashCode() == websocket.GetHashCode());
        }
    }
}
