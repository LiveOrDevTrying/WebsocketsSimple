using WebsocketsSimple.Core.Models;

namespace WebsocketsSimple.Server.Models
{
    public interface IConnectionWSServer : IConnectionWS
    {
        bool HasBeenPinged { get; set; }
        string ConnectionId { get; set; }
        string[] SubProtocols { get; set; }
    }
}