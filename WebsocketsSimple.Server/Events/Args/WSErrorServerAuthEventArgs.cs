using WebsocketsSimple.Server.Models;

namespace WebsocketsSimple.Server.Events.Args
{
    public class WSErrorServerAuthEventArgs<T> : WSErrorServerBaseEventArgs<IdentityWSServer<T>>
    {
    }
}
