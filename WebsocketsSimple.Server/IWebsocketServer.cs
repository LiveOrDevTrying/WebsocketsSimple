using WebsocketsSimple.Server.Events.Args;
using WebsocketsSimple.Server.Models;

namespace WebsocketsSimple.Server
{
    public interface IWebsocketServer 
        : IWebsocketServerBase<
            WSConnectionServerEventArgs<ConnectionWSServer>, 
            WSMessageServerEventArgs<ConnectionWSServer>, 
            WSErrorServerEventArgs<ConnectionWSServer>, 
            ConnectionWSServer>
    {
    }
}