using PHS.Networking.Enums;
using PHS.Networking.Server.Managers;
using PHS.Networking.Server.Services;
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebsocketsSimple.Server.Events.Args;
using WebsocketsSimple.Server.Handlers;
using WebsocketsSimple.Server.Models;

namespace WebsocketsSimple.Server
{
    public abstract class WebsocketServerAuthBase<T, U, V, W, X, Y, Z, A, B> :
        WebsocketServerBase<T, U, V, W, X, Y, Z>
        where T : WSConnectionServerAuthBaseEventArgs<Z, A>
        where U : WSMessageServerAuthBaseEventArgs<Z, A>
        where V : WSErrorServerAuthBaseEventArgs<Z, A>
        where W : ParamsWSServerAuth
        where X : WebsocketHandlerAuthBase<T, U, V, W, B, Z, A>
        where Y : ConnectionManagerAuth<Z, A>
        where Z : IdentityWSServer<A>
        where B : WSAuthorizeBaseEventArgs<Z, A>
    {
        protected readonly IUserService<A> _userService;

        public WebsocketServerAuthBase(W parameters,
            IUserService<A> userService) : base(parameters)
        {
            _userService = userService;

            _handler.AuthorizeEvent += OnAuthorizeEvent;
        }

        public WebsocketServerAuthBase(W parameters,
            IUserService<A> userService,
            byte[] certificate,
            string certificatePassword) : base(parameters, certificate, certificatePassword)
        {
            _userService = userService;

            _handler.AuthorizeEvent += OnAuthorizeEvent;
        }

        public virtual async Task<bool> SendToUserAsync(string message, A userId, CancellationToken cancellationToken = default)
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
        public virtual async Task<bool> SendToUserAsync(byte[] message, A userId, CancellationToken cancellationToken = default)
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
        protected override void OnConnectionEvent(object sender, T args)
        {
            switch (args.ConnectionEventType)
            {
                case ConnectionEventType.Connected:
                    if (!_connectionManager.AddUser(args.Connection))
                    {
                        FireEvent(this, args);

                        Task.Run(async () =>
                        {
                            await DisconnectConnectionAsync(args.Connection, args.CancellationToken).ConfigureAwait(false);
                        });
                        return;
                    }
                    break;
                case ConnectionEventType.Disconnect:
                    _connectionManager.RemoveConnection(args.Connection.ConnectionId);
                    break;
                default:
                    break;
            }

            FireEvent(this, args);
        }
        protected virtual void OnAuthorizeEvent(object sender, B args)
        {
            Task.Run(async () =>
            {
                try
                {
                    if (args.Token == null || !await _userService.IsValidTokenAsync(args.Token, args.CancellationToken).ConfigureAwait(false))
                    {
                        var bytes = Encoding.UTF8.GetBytes(_parameters.ConnectionUnauthorizedString);

                        if (args.Connection.SslStream != null)
                        {
                            await args.Connection.SslStream.WriteAsync(bytes, args.CancellationToken).ConfigureAwait(false);
                        }
                        else
                        {
                            await args.Connection.TcpClient.Client.SendAsync(new ArraySegment<byte>(bytes), SocketFlags.None, args.CancellationToken).ConfigureAwait(false);
                        }
                        await DisconnectConnectionAsync(args.Connection, statusDescription: _parameters.ConnectionUnauthorizedString, cancellationToken: args.CancellationToken).ConfigureAwait(false);
                        return;
                    }

                    args.Connection.UserId = await _userService.GetIdAsync(args.Token, args.CancellationToken);

                    await _handler.AuthorizeCallbackAsync(args, args.CancellationToken).ConfigureAwait(false);
                } 
                catch (Exception ex) 
                {
                    FireEvent(this, CreateErrorEventArgs(new WSErrorServerBaseEventArgs<Z>
                    {
                        Exception = ex,
                        Message = ex.Message,
                        Connection = args.Connection,
                        CancellationToken = args.CancellationToken
                    }));
                }
            }, args.CancellationToken);
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
