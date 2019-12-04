using PHS.Core.Models;
using System.Collections.Generic;
using System.Net.WebSockets;
using WebsocketsSimple.Core.Models;

namespace WebsocketsSimple.Server.Models
{
    public interface IUserConnectionWSDTO : IUserConnectionDTO
    {
        ICollection<ConnectionWebsocketDTO> Connections { get; set; }

        ConnectionWebsocketDTO GetConnection(WebSocket websocket);
    }
}