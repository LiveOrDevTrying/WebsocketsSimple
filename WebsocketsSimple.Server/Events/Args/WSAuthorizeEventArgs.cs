using WebsocketsSimple.Server.Models;

namespace WebsocketsSimple.Server.Events.Args
{
    public class WSAuthorizeEventArgs<A> : WSAuthorizeBaseEventArgs<IdentityWSServer<A>, A>
    {
    }
}

