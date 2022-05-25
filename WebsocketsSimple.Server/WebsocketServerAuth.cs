using PHS.Networking.Enums;
using PHS.Networking.Events;
using PHS.Networking.Models;
using PHS.Networking.Server.Enums;
using PHS.Networking.Server.Events.Args;
using PHS.Networking.Server.Services;
using PHS.Networking.Services;
using System;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebsocketsSimple.Server.Events.Args;
using WebsocketsSimple.Server.Handlers;
using WebsocketsSimple.Server.Managers;
using WebsocketsSimple.Server.Models;

namespace WebsocketsSimple.Server
{
    public class WebsocketServerAuth<T> :
        WebsocketServerBase<
            WSConnectionServerAuthEventArgs<T>, 
            WSMessageServerAuthEventArgs<T>, 
            WSErrorServerAuthEventArgs<T>, 
            ParamsWSServerAuth,
            WebsocketHandlerAuth<T>, 
            WSConnectionManagerAuth<T>,
            IdentityWSServer<T>>, 
        IWebsocketServerAuth<T>
    {
        protected readonly IUserService<T> _userService;

        public WebsocketServerAuth(ParamsWSServerAuth parameters,
            IUserService<T> userService) : base(parameters)
        {
            _userService = userService;

            _handler.AuthorizeEvent += OnAuthorizeEvent;
        }

        public WebsocketServerAuth(ParamsWSServerAuth parameters,
            IUserService<T> userService,
            byte[] certificate,
            string certificatePassword) : base(parameters, certificate, certificatePassword)
        {
            _userService = userService;

            _handler.AuthorizeEvent += OnAuthorizeEvent;
        }

        public virtual async Task<bool> SendToUserAsync(string message, T userId, IdentityWSServer<T> connectionSending = null, CancellationToken cancellationToken = default)
        {
            if (IsServerRunning)
            {
                foreach (var connection in _connectionManager.GetAll(userId))
                {
                    await SendToConnectionAsync(message, connection, cancellationToken);
                }

                return true;
            }

            return false;
        }
        public virtual async Task<bool> SendToUserAsync(byte[] message, T userId, IdentityWSServer<T> connectionSending = null, CancellationToken cancellationToken = default)
        {
            if (IsServerRunning)
            {
                foreach (var connection in _connectionManager.GetAll(userId))
                {
                    await SendToConnectionAsync(message, connection, cancellationToken);
                }

                return true;
            }

            return false;
        }

        public override async Task<bool> SendToConnectionAsync(string message, IdentityWSServer<T> connection, CancellationToken cancellationToken = default)
        {
            if (IsServerRunning)
            {
                return await _handler.SendAsync(message, connection, cancellationToken);
            }

            return false;
        }

        protected override void OnConnectionEvent(object sender, WSConnectionServerBaseEventArgs<IdentityWSServer<T>> args)
        {
            var connection = _connectionManager.Get(args.Connection.ConnectionId);

            switch (args.ConnectionEventType)
            {
                case ConnectionEventType.Connected:
                    _connectionManager.Add(args.Connection);
                    break;
                case ConnectionEventType.Disconnect:
                    _connectionManager.Remove(args.Connection.ConnectionId);
                    break;
                default:
                    break;
            }

            FireEvent(this, new WSConnectionServerAuthEventArgs<T>
            {
                ConnectionEventType = args.ConnectionEventType,
                Connection = connection,
            });
        }
        protected override void OnMessageEvent(object sender, WSMessageServerBaseEventArgs<IdentityWSServer<T>> args)
        {
            FireEvent(this, new WSMessageServerAuthEventArgs<T>
            {
                MessageEventType = args.MessageEventType,
                Message = args.Message,
                Bytes = args.Bytes,
                Connection = args.Connection,
            });
        }
        protected override void OnErrorEvent(object sender, WSErrorServerBaseEventArgs<IdentityWSServer<T>> args)
        {
            FireEvent(this, new WSErrorServerAuthEventArgs<T>
            {
                Exception = args.Exception,
                Message = args.Message,
                Connection = args.Connection
            });
        }
        protected virtual void OnAuthorizeEvent(object sender, WSAuthorizeEventArgs<T> args)
        {
            Task.Run(async () =>
            {
                var token = args.Connection.QueryStringParameters.FirstOrDefault(x => x.Key.Trim().ToLower() == "token");

                if (token.Value == null || !await _userService.IsValidTokenAsync(token.Value, _cancellationToken))
                {
                    var bytes = Encoding.UTF8.GetBytes(_parameters.ConnectionUnauthorizedString);
                    await args.Connection.Stream.WriteAsync(bytes, _cancellationToken);
                    await DisconnectConnectionAsync(args.Connection, _cancellationToken);
                    return;
                }

                args.Connection.UserId = await _userService.GetIdAsync(token.Value, _cancellationToken);

                await _handler.UpgradeConnectionCallbackAsync(args, _cancellationToken);
            }, _cancellationToken);
        }
        
        protected override WebsocketHandlerAuth<T> CreateWebsocketHandler(byte[] certificate = null, string certificatePassword = null)
        {
            return certificate != null
                ? new WebsocketHandlerAuth<T>(_parameters, certificate, certificatePassword)
                : new WebsocketHandlerAuth<T>(_parameters);
        }
        protected override WSConnectionManagerAuth<T> CreateWSConnectionManager()
        {
            return new WSConnectionManagerAuth<T>();
        }

        public override void Dispose()
        {
            foreach (var connection in _connectionManager.GetAll())
            {
                try
                {
                    DisconnectConnectionAsync(connection).Wait();
                }
                catch
                { }
            }

            if (_handler != null)
            {
                _handler.AuthorizeEvent -= OnAuthorizeEvent;
            }

            base.Dispose();
        }
    }
}
