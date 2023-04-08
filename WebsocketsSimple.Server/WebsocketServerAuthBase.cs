using PHS.Networking.Server.Services;
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
    public abstract class WebsocketServerAuthBase<T, U, V, W, X, Y, Z, A, B> :
        WebsocketServerBase<T, U, V, W, X, Y, Z>
        where T : WSConnectionServerAuthBaseEventArgs<Z, A>
        where U : WSMessageServerAuthBaseEventArgs<Z, A>
        where V : WSErrorServerAuthBaseEventArgs<Z, A>
        where W : ParamsWSServerAuth
        where X : WebsocketHandlerAuthBase<T, U, V, W, B, Z, A>
        where Y : WSConnectionManagerAuthBase<Z, A>
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

        protected virtual void OnAuthorizeEvent(object sender, B args)
        {
            Task.Run(async () =>
            {
                try
                {
                    if (args.Token == null || args.Token.Length <= 0 || !await _userService.IsValidTokenAsync(args.Token, _cancellationToken).ConfigureAwait(false))
                    {
                        var bytes = Encoding.UTF8.GetBytes(_parameters.ConnectionUnauthorizedString);
                        await args.Connection.TcpClient.Client.SendAsync(new ArraySegment<byte>(bytes), SocketFlags.None, _cancellationToken).ConfigureAwait(false);
                        await DisconnectConnectionAsync(args.Connection, statusDescription: _parameters.ConnectionUnauthorizedString, cancellationToken: _cancellationToken).ConfigureAwait(false);
                        return;
                    }

                    args.Connection.UserId = await _userService.GetIdAsync(args.Token, _cancellationToken);

                    _connectionManager.Add(args.Connection);

                    if (!_parameters.OnlyEmitBytes || !string.IsNullOrWhiteSpace(_parameters.ConnectionSuccessString))
                    {
                        await SendToConnectionAsync(_parameters.ConnectionSuccessString, args.Connection, _cancellationToken).ConfigureAwait(false);
                    }

                    await _handler.AuthorizeCallbackAsync(args, _cancellationToken).ConfigureAwait(false);
                } 
                catch (Exception ex) 
                {
                    FireEvent(this, CreateErrorEventArgs(new WSErrorServerBaseEventArgs<Z>
                    {
                        Exception = ex,
                        Message = ex.Message,
                        Connection = args.Connection
                    }));
                }
            }, _cancellationToken);
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
