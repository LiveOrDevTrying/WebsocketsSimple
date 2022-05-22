using WebsocketsSimple.Server.Events.Args;
using WebsocketsSimple.Server.Models;

namespace WebsocketsSimple.Server
{
    public interface IWebsocketServer : 
        IWebsocketServerBase<
            WSConnectionServerEventArgs, 
            WSMessageServerEventArgs, 
            WSErrorServerEventArgs, 
            ConnectionWSServer>
    {
    }
}