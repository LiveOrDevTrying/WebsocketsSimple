using WebsocketsSimple.Core.Models;

namespace WebsocketsSimple.Server.Models
{
    public interface IConnectionServer : IConnection
    {
        bool HasBeenPinged { get; set; }
        string ConnectionId { get; set; }
    }
}
