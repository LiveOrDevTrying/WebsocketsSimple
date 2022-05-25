using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using WebsocketsSimple.Server.Events.Args;
using WebsocketsSimple.Server.Models;

namespace WebsocketsSimple.Server.Handlers
{
    public delegate void WebsocketAuthorizeEvent<T>(object sender, WSAuthorizeEventArgs<T> args);

    public class WebsocketHandlerAuth<T> : WebsocketHandlerBase<IdentityWSServer<T>>
    {
        private event WebsocketAuthorizeEvent<T> _authorizeEvent;

        public WebsocketHandlerAuth(ParamsWSServer parameters) : base(parameters)
        {
        }
        public WebsocketHandlerAuth(ParamsWSServer parameters, byte[] certificate, string certificatePassword)
            : base(parameters, certificate, certificatePassword)
        {
        }
        
        protected override IdentityWSServer<T> CreateConnection(TcpClient client, Stream stream)
        {
            return new IdentityWSServer<T>
            {
                ConnectionId = Guid.NewGuid().ToString(),
                Stream = stream,
                Client = client
            };
        }
        protected override Task UpgradeConnectionAsync(string message, string[] requestedSubprotocols, IdentityWSServer<T> connection, CancellationToken cancellationToken)
        {
            SetPathAndQueryStringForConnection(message, connection);

            FireEvent(this, new WSAuthorizeEventArgs<T>
            {
                Connection = connection,
                UpgradeData = message,
                RequestedSubprotocols = requestedSubprotocols
            });

            return Task.CompletedTask;
        }
        public virtual async Task UpgradeConnectionCallbackAsync(WSAuthorizeEventArgs<T> args, CancellationToken cancellationToken)
        {
            await base.UpgradeConnectionAsync(args.UpgradeData, args.RequestedSubprotocols, args.Connection, cancellationToken).ConfigureAwait(false);
        }

        protected virtual void FireEvent(object sender, WSAuthorizeEventArgs<T> args)
        {
            _authorizeEvent?.Invoke(sender, args);
        }

        public event WebsocketAuthorizeEvent<T> AuthorizeEvent
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