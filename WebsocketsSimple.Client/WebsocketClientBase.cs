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
        protected X _handler;
        protected W _parameters;
        protected string _token;
        protected Uri _uri;

        public WebsocketClientBase(W parameters, string token = "")
        {
            _parameters = parameters;
            _token = token;

            _handler = CreateWebsocketHandler();
            _handler.ConnectionEvent += OnConnectionEvent;
            _handler.MessageEvent += OnMessageEvent;
            _handler.ErrorEvent += OnErrorEvent;
        }

        public virtual async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
        {
            return await _handler.ConnectAsync(cancellationToken);
        }
        public virtual async Task<bool> DisconnectAsync(CancellationToken cancellationToken = default)
        {
            return await _handler.DisconnectAsync(cancellationToken);
        }
        
        public virtual async Task<bool> SendAsync(string message, CancellationToken cancellationToken = default)
        {
            return await _handler.SendAsync(message, cancellationToken);
        }
        public virtual async Task<bool> SendAsync(byte[] message, CancellationToken cancellationToken = default)
        {
            return await _handler.SendAsync(message, cancellationToken);
        }

        protected abstract void OnConnectionEvent(object sender, WSConnectionClientEventArgs args);
        protected abstract void OnMessageEvent(object sender, WSMessageClientEventArgs args);
        protected abstract void OnErrorEvent(object sender, WSErrorClientEventArgs args);

        protected abstract X CreateWebsocketHandler();

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
                    _handler.Connection.Websocket != null &&
                    _handler.Connection.Websocket.State == WebSocketState.Open;
            }
        }
        public IConnection Connection
        {
            get
            {
                return _handler.Connection;
            }
        }
    }
}
