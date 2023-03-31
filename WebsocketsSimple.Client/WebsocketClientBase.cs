using PHS.Networking.Services;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using WebsocketsSimple.Client.Models;
using WebsocketsSimple.Core.Events.Args;
using WebsocketsSimple.Core.Models;

namespace WebsocketsSimple.Client
{
    public abstract class WebsocketClientBase<T, U, V, W, X, Y> :
        CoreNetworkingGeneric<T, U, V, W, Y>,
        ICoreNetworkingClient<T, U, V, Y>
        where T : WSConnectionEventArgs<Y>
        where U : WSMessageEventArgs<Y>
        where V : WSErrorEventArgs<Y>
        where W : ParamsWSClient
        where X : WebsocketClientHandlerBase<T, U, V, W, Y>
        where Y : ConnectionWS
    {
        protected readonly X _handler;

        public WebsocketClientBase(W parameters) : base(parameters)
        {
            _handler = CreateWebsocketClientHandler();
            _handler.ConnectionEvent += OnConnectionEvent;
            _handler.MessageEvent += OnMessageEvent;
            _handler.ErrorEvent += OnErrorEvent;
        }

        public virtual async Task<bool> ConnectAsync(CancellationToken? cancellationToken = default)
        {
            if (cancellationToken.HasValue)
            {
                return await _handler.ConnectAsync(cancellationToken.Value).ConfigureAwait(false);
            }

            return await _handler.ConnectAsync(default(CancellationToken)).ConfigureAwait(false);
        }
        public virtual async Task<bool> DisconnectAsync(WebSocketCloseStatus webSocketCloseStatus = WebSocketCloseStatus.NormalClosure,
            string closeStatusDescription = "Disconnect", 
            CancellationToken cancellationToken = default)
        {
            return await _handler.DisconnectAsync(webSocketCloseStatus, closeStatusDescription, cancellationToken).ConfigureAwait(false);
        }
        public virtual async Task<bool> DisconnectAsync(CancellationToken? cancellationToken = default)
        {
            if (cancellationToken.HasValue)
            {
                return await _handler.DisconnectAsync(cancellationToken: cancellationToken.Value).ConfigureAwait(false);
            }

            return await _handler.DisconnectAsync().ConfigureAwait(false);
        }

        public virtual async Task<bool> SendAsync(string message, CancellationToken? cancellationToken = default)
        {
            if (cancellationToken.HasValue)
            {
                return await _handler.SendAsync(message, cancellationToken.Value).ConfigureAwait(false);
            }

            return await _handler.SendAsync(message, default(CancellationToken)).ConfigureAwait(false);
        }
        public virtual async Task<bool> SendAsync(byte[] message, CancellationToken? cancellationToken = default)
        {
            if (cancellationToken.HasValue)
            {
                return await _handler.SendAsync(message, cancellationToken.Value).ConfigureAwait(false);
            }

            return await _handler.SendAsync(message, default(CancellationToken)).ConfigureAwait(false);
        }

        protected virtual void OnConnectionEvent(object sender, T args)
        {
            FireEvent(sender, args);
        }
        protected virtual void OnMessageEvent(object sender, U args)
        {
            FireEvent(sender, args);
        }
        protected virtual void OnErrorEvent(object sender, V args)
        {
            FireEvent(sender, args);
        }

        protected abstract X CreateWebsocketClientHandler();

        public override void Dispose()
        {
            if (_handler != null)
            {
                _handler.ConnectionEvent -= OnConnectionEvent;
                _handler.MessageEvent -= OnMessageEvent;
                _handler.ErrorEvent -= OnErrorEvent;
                _handler.Dispose();
            }
        }

        public bool IsRunning
        {
            get
            {
                return _handler.Connection != null &&
                    _handler.Connection.TcpClient != null &&
                    _handler.Connection.TcpClient.Connected &&
                    _handler.Connection.Websocket != null &&
                    _handler.Connection.Websocket.State == WebSocketState.Open;
            }
        }
        public Y Connection
        {
            get
            {
                return _handler.Connection;
            }
        }
    }
}
