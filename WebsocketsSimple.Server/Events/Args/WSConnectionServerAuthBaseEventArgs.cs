using WebsocketsSimple.Server.Models;

namespace WebsocketsSimple.Server.Events.Args
{
    public class WSConnectionServerAuthBaseEventArgs<Z, A> : WSConnectionServerBaseEventArgs<Z>
        where Z : IdentityWSServer<A>
    {
    }
}

