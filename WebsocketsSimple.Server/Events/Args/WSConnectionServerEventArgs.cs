using WebsocketsSimple.Core.Events.Args;
using WebsocketsSimple.Server.Models;

namespace WebsocketsSimple.Server.Events.Args
{
    public class WSConnectionServerEventArgs<T> : WSConnectionEventArgs<T> where T : ConnectionWSServer
    {
    }
}

