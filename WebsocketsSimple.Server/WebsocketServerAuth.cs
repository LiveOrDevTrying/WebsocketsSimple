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

        public virtual async Task<bool> SendToUserAsync(string message, T userId, CancellationToken cancellationToken = default)
        {
            if (IsServerRunning)
            {
                foreach (var connection in _connectionManager.GetAll(userId))
                {
                    await SendToConnectionAsync(message, connection, cancellationToken).ConfigureAwait(false);
                }

                return true;
            }

            return false;
        }
        public virtual async Task<bool> SendToUserAsync(byte[] message, T userId, CancellationToken cancellationToken = default)
        {
            if (IsServerRunning)
            {
                foreach (var connection in _connectionManager.GetAll(userId))
                {
                    await SendToConnectionAsync(message, connection, cancellationToken).ConfigureAwait(false);
                }

                return true;
            }

            return false;
        }

        protected override void OnConnectionEvent(object sender, WSConnectionServerAuthEventArgs<T> args)
        {
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

            FireEvent(this, args);
        }
        protected override void OnMessageEvent(object sender, WSMessageServerAuthEventArgs<T> args)
        {
            FireEvent(this, args);
        }
        protected override void OnErrorEvent(object sender, WSErrorServerAuthEventArgs<T> args)
        {
            FireEvent(this, args);
        }
        protected virtual void OnAuthorizeEvent(object sender, WSAuthorizeEventArgs<T> args)
        {
            Task.Run(async () =>
            {
                try
                {
                    var token = args.Connection.QueryStringParameters.FirstOrDefault(x => x.Key.Trim().ToLower() == "token");

                    if (token.Value == null || !await _userService.IsValidTokenAsync(token.Value, _cancellationToken).ConfigureAwait(false))
                    {
                        var bytes = Encoding.UTF8.GetBytes(_parameters.ConnectionUnauthorizedString);
                        await args.Connection.TcpClient.Client.SendAsync(new ArraySegment<byte>(bytes), SocketFlags.None, _cancellationToken).ConfigureAwait(false);
                        await DisconnectConnectionAsync(args.Connection, statusDescription: _parameters.ConnectionUnauthorizedString, cancellationToken: _cancellationToken).ConfigureAwait(false);
                        return;
                    }

                    args.Connection.UserId = await _userService.GetIdAsync(token.Value, _cancellationToken).ConfigureAwait(false);

                    await _handler.AuthorizedConnectionCallback(args, _cancellationToken).ConfigureAwait(false);
                } 
                catch (Exception ex) 
                {
                    FireEvent(this, new WSErrorServerAuthEventArgs<T>
                    {
                        Exception = ex,
                        Message = ex.Message,
                        Connection = args.Connection
                    });
                }
            }, _cancellationToken);
        }
        
        protected override WebsocketHandlerAuth<T> CreateHandler(byte[] certificate = null, string certificatePassword = null)
        {
            return certificate != null
                ? new WebsocketHandlerAuth<T>(_parameters, certificate, certificatePassword)
                : new WebsocketHandlerAuth<T>(_parameters);
        }
        protected override WSConnectionManagerAuth<T> CreateConnectionManager()
        {
            return new WSConnectionManagerAuth<T>();
        }

        public override void Dispose()
        {
            if (_handler != null)
            {
                _handler.AuthorizeEvent -= OnAuthorizeEvent;
            }

            base.Dispose();
        }
    }
}
