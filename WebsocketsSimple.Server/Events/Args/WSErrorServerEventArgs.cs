using WebsocketsSimple.Core.Events.Args;
using WebsocketsSimple.Server.Models;

namespace WebsocketsSimple.Server.Events.Args
{
    public class WSErrorServerEventArgs<T> : WSErrorEventArgs<T> where T : ConnectionWSServer
    {
    }
}
