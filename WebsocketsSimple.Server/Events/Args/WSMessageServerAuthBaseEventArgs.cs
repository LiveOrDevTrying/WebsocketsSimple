using WebsocketsSimple.Server.Models;

namespace WebsocketsSimple.Server.Events.Args
{
    public class WSMessageServerAuthBaseEventArgs<Z, A> : WSMessageServerBaseEventArgs<Z>
        where Z : IdentityWSServer<A>
    {
    }
}
