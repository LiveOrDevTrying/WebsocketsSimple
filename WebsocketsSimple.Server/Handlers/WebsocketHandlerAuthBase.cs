using System.Threading;
using System.Threading.Tasks;
using WebsocketsSimple.Server.Events.Args;
using WebsocketsSimple.Server.Models;

namespace WebsocketsSimple.Server.Handlers
{
    public delegate void WebsocketAuthorizeEvent<Z, X, A>(object sender, X args) 
        where X : WSAuthorizeBaseEventArgs<Z, A> 
        where Z : IdentityWSServer<A>;

    public abstract class WebsocketHandlerAuthBase<T, U, V, W, X, Z, A> : 
        WebsocketHandlerBase<T, U, V, W, Z>
        where T : WSConnectionServerAuthBaseEventArgs<Z, A>
        where U : WSMessageServerAuthBaseEventArgs<Z, A>
        where V : WSErrorServerAuthBaseEventArgs<Z, A>
        where W : ParamsWSServerAuth
        where X : WSAuthorizeBaseEventArgs<Z, A>
        where Z : IdentityWSServer<A>
    {
        private event WebsocketAuthorizeEvent<Z, X, A> _authorizeEvent;

        public WebsocketHandlerAuthBase(W parameters) : base(parameters)
        {
        }
        public WebsocketHandlerAuthBase(W parameters, byte[] certificate, string certificatePassword) : base(parameters, certificate, certificatePassword)
        {
        }

        public virtual async Task AuthorizeCallbackAsync(X args, CancellationToken cancellationToken)
        {
            await base.UpgradeConnectionAsync(args.UpgradeData, args.RequestedSubprotocols, args.Connection, cancellationToken).ConfigureAwait(false);
        }

        protected virtual void FireEvent(object sender, X args)
        {
            _authorizeEvent?.Invoke(sender, args);
        }

        public event WebsocketAuthorizeEvent<Z, X, A> AuthorizeEvent
        {
            add
            {
                _authorizeEvent += value;
            }
            remove
            {
                _authorizeEvent -= value;
            }
        }
    }
}