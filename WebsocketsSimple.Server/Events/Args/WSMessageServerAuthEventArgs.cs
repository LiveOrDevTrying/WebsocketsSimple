using WebsocketsSimple.Server.Models;

namespace WebsocketsSimple.Server.Events.Args
{
    public class WSMessageServerAuthEventArgs<T> : WSMessageServerBaseEventArgs<IdentityWSServer<T>>
    {
    }
}
