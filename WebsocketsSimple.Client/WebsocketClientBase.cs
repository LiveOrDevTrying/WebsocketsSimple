using PHS.Networking.Models;
using PHS.Networking.Services;
using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using WebsocketsSimple.Client.Events.Args;
using WebsocketsSimple.Client.Models;
using WebsocketsSimple.Core.Models;

namespace WebsocketsSimple.Client
{
    public abstract class WebsocketClientBase<T, U, V, W, X, Y> :
        CoreNetworking<T, U, V>,
        ICoreNetworking<T, U, V>
        where T : WSConnectionClientEventArgs
        where U : WSMessageClientEventArgs
        where V : WSErrorClientEventArgs
        where W : ParamsWSClient
        where X : WebsocketClientHandlerBase<Y>
        where Y : ConnectionWS
    {
        protected readonly X _handler;
        protected readonly W _parameters;
        protected readonly string _token;

        public WebsocketClientBase(W parameters, string token = "")
        {
            _parameters = parameters;
            _token = token;

            _handler = CreateWebsocketClientHandler();
            _handler.ConnectionEvent += OnConnectionEvent;
            _handler.MessageEvent += OnMessageEvent;
            _handler.ErrorEvent += OnErrorEvent;
        }

        public virtual async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
        {
            return await _handler.ConnectAsync(cancellationToken).ConfigureAwait(false);
        }
        public virtual async Task<bool> DisconnectAsync(WebSocketCloseStatus webSocketCloseStatus = WebSocketCloseStatus.NormalClosure,
            string closeStatusDescription = "Disconnect", 
            CancellationToken cancellationToken = default)
        {
            return await _handler.DisconnectAsync(webSocketCloseStatus, closeStatusDescription, cancellationToken).ConfigureAwait(false);
        }
        
        public virtual async Task<bool> SendAsync(string message, CancellationToken cancellationToken = default)
        {
            return await _handler.SendAsync(message, cancellationToken).ConfigureAwait(false);
        }
        public virtual async Task<bool> SendAsync(byte[] message, CancellationToken cancellationToken = default)
        {
            return await _handler.SendAsync(message, cancellationToken).ConfigureAwait(false);
        }

        protected abstract void OnConnectionEvent(object sender, WSConnectionClientEventArgs args);
        protected abstract void OnMessageEvent(object sender, WSMessageClientEventArgs args);
        protected abstract void OnErrorEvent(object sender, WSErrorClientEventArgs args);

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

            base.Dispose();
        }

        public bool IsRunning
        {
            get
            {
                return _handler.Connection != null &&
                    _handler.Connection.Client != null &&
                    _handler.Connection.Client.Connected &&
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
