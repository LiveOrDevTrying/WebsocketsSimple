using WebsocketsSimple.Server.Models;

namespace WebsocketsSimple.Server.Events.Args
{
    public class WSErrorServerAuthEventArgs<T> : WSErrorServerAuthBaseEventArgs<IdentityWSServer<T>, T>
    {
    }
}
