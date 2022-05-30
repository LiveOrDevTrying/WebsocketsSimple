using PHS.Networking.Events.Args;
using PHS.Networking.Models;
using System;
using System.Threading;
using System.Threading.Tasks;
using WebsocketsSimple.Server.Events.Args;
using WebsocketsSimple.Server.Models;

namespace WebsocketsSimple.Server.Handlers
{
    public delegate void WebsocketAuthorizeEvent<T>(object sender, WSAuthorizeEventArgs<T> args);

    public class WebsocketHandlerAuth<T> : 
        WebsocketHandlerBase<
            WSConnectionServerAuthEventArgs<T>,
            WSMessageServerAuthEventArgs<T>,
            WSErrorServerAuthEventArgs<T>,
            ParamsWSServerAuth,
            IdentityWSServer<T>>
    {
        private event WebsocketAuthorizeEvent<T> _authorizeEvent;

        public WebsocketHandlerAuth(ParamsWSServerAuth parameters) : base(parameters)
        {
        }
        public WebsocketHandlerAuth(ParamsWSServerAuth parameters, byte[] certificate, string certificatePassword)
            : base(parameters, certificate, certificatePassword)
        {
        }
        
        protected override IdentityWSServer<T> CreateConnection(ConnectionTcpClient connection)
        {
            return new IdentityWSServer<T>
            {
                ConnectionId = Guid.NewGuid().ToString(),
                TcpClient = connection.TcpClient
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
        public virtual async Task AuthorizedConnectionCallback(WSAuthorizeEventArgs<T> args, CancellationToken cancellationToken)
        {
            await base.UpgradeConnectionAsync(args.UpgradeData, args.RequestedSubprotocols, args.Connection, cancellationToken).ConfigureAwait(false);
        }

        protected override WSMessageServerAuthEventArgs<T> CreateMessageEventArgs(WSMessageServerBaseEventArgs<IdentityWSServer<T>> args)
        {
            return new WSMessageServerAuthEventArgs<T>
            {
                Bytes = args.Bytes,
                Connection = args.Connection,
                Message = args.Message,
                MessageEventType = args.MessageEventType
            };
        }
        protected override WSConnectionServerAuthEventArgs<T> CreateConnectionEventArgs(ConnectionEventArgs<IdentityWSServer<T>> args)
        {
            return new WSConnectionServerAuthEventArgs<T>
            {
                Connection = args.Connection,
                ConnectionEventType = args.ConnectionEventType
            };
        }

        protected override WSErrorServerAuthEventArgs<T> CreateErrorEventArgs(ErrorEventArgs<IdentityWSServer<T>> args)
        {
            return new WSErrorServerAuthEventArgs<T>
            {
                Connection = args.Connection,
                Exception = args.Exception,
                Message = args.Message
            };
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