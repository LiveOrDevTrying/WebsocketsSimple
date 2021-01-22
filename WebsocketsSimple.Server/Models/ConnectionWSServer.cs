using WebsocketsSimple.Core.Models;

namespace WebsocketsSimple.Server.Models
{
    public class ConnectionWSServer : ConnectionWS, IConnectionWSServer
    {
        public bool HasBeenPinged { get; set; }
        public string ConnectionId { get; set; }
    }
}
