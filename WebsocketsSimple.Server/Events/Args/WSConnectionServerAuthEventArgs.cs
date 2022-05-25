using WebsocketsSimple.Server.Models;

namespace WebsocketsSimple.Server.Events.Args
{
    public class WSConnectionServerAuthEventArgs<T> : WSConnectionServerBaseEventArgs<IdentityWSServer<T>>
    {
    }
}

