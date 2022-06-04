using WebsocketsSimple.Server.Models;

namespace WebsocketsSimple.Server.Events.Args
{
    public class WSMessageServerAuthEventArgs<T> : WSMessageServerAuthBaseEventArgs<IdentityWSServer<T>, T>
    {
    }
}
