using WebsocketsSimple.Server.Models;

namespace WebsocketsSimple.Server.Events.Args
{
    public class WSErrorServerAuthBaseEventArgs<Z, A> : WSErrorServerBaseEventArgs<Z>
        where Z : IdentityWSServer<A>
    {
    }
}
