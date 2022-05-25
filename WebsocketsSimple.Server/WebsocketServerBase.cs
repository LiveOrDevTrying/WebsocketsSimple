using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using PHS.Networking.Server.Events.Args;
using PHS.Networking.Services;
using PHS.Networking.Events;
using WebsocketsSimple.Server.Events.Args;
using WebsocketsSimple.Server.Handlers;
using WebsocketsSimple.Server.Models;
using WebsocketsSimple.Server.Managers;
using System.Collections.Generic;
using WebsocketsSimple.Core.Events.Args;
using System.Net.WebSockets;

namespace WebsocketsSimple.Server
{
    public abstract class WebsocketServerBase<T, U, V, W, X, Y, Z> : 
        CoreNetworking<T, U, V>,
        ICoreNetworking<T, U, V>
        where T : WSConnectionEventArgs<Z>
        where U : WSMessageEventArgs<Z>
        where V : WSErrorEventArgs<Z>
        where W : ParamsWSServer
        where X : WebsocketHandlerBase<Z>
        where Y : WSConnectionManager<Z>
        where Z : ConnectionWSServer
    {
        protected readonly X _handler;
        protected readonly W _parameters;
        protected readonly Y _connectionManager;
        protected volatile bool _isPingRunning;
        protected CancellationToken _cancellationToken;

        protected event NetworkingEventHandler<ServerEventArgs> _serverEvent;

        public WebsocketServerBase(W parameters)
        {
            _parameters = parameters;
            _connectionManager = CreateWSConnectionManager();

            _handler = CreateWebsocketHandler();
            _handler.ConnectionEvent += OnConnectionEvent;
            _handler.MessageEvent += OnMessageEvent;
            _handler.ErrorEvent += OnErrorEvent;
            _handler.ServerEvent += OnServerEvent;
        }
        public WebsocketServerBase(W parameters,
            byte[] certificate,
            string certificatePassword)
        {
            _parameters = parameters;
            _connectionManager = CreateWSConnectionManager();

            _handler = CreateWebsocketHandler(certificate, certificatePassword);
            _handler.ConnectionEvent += OnConnectionEvent;
            _handler.MessageEvent += OnMessageEvent;
            _handler.ErrorEvent += OnErrorEvent;
            _handler.ServerEvent += OnServerEvent;
        }
 
        public virtual void Start(CancellationToken cancellationToken = default)
        {
            _cancellationToken = cancellationToken;
            _handler.Start(cancellationToken);
        }
        public virtual void Stop()
        {
            _handler.Stop();
        }

        public virtual async Task<bool> BroadcastToAllConnectionsAsync(string message, Z connectionSending = null, CancellationToken cancellationToken = default)
        {
            if (IsServerRunning)
            {
                foreach (var connection in _connectionManager.GetAll())
                {
                    if (connectionSending == null || connection.ConnectionId != connectionSending.ConnectionId)
                    {
                        await SendToConnectionAsync(message, connection, cancellationToken).ConfigureAwait(false);
                    }
                }

                return true;
            }

            return false;
        }
        public virtual async Task<bool> BroadcastToAllConnectionsAsync(byte[] message, Z connectionSending = null, CancellationToken cancellationToken = default)
        {
            if (IsServerRunning)
            {
                foreach (var connection in _connectionManager.GetAll())
                {
                    if (connectionSending == null || connection.ConnectionId != connectionSending.ConnectionId)
                    {
                        await SendToConnectionAsync(message, connection, cancellationToken).ConfigureAwait(false);
                    }
                }

                return true;
            }

            return false;
        }

        public virtual async Task<bool> SendToConnectionAsync(string message, Z connection, CancellationToken cancellationToken = default)
        {
            if (IsServerRunning)
            {
                return await _handler.SendAsync(message, connection, cancellationToken).ConfigureAwait(false);
            }
            
            return false;
        }
        public virtual async Task<bool> SendToConnectionAsync(byte[] message, Z connection, CancellationToken cancellationToken = default)
        {
            if (IsServerRunning)
            {
                return await _handler.SendAsync(message, connection, cancellationToken).ConfigureAwait(false);
            }
            
            return false;
        }

        public virtual async Task DisconnectConnectionAsync(Z connection,
            WebSocketCloseStatus webSocketCloseStatus = WebSocketCloseStatus.NormalClosure,
            string statusDescription = "Disconnect",
            CancellationToken cancellationToken = default)
        {
            await _handler.DisconnectConnectionAsync(connection, webSocketCloseStatus, statusDescription, cancellationToken).ConfigureAwait(false);
        }

        protected abstract void OnConnectionEvent(object sender, WSConnectionServerBaseEventArgs<Z> args);
        protected virtual void OnServerEvent(object sender, ServerEventArgs args)
        {
            FireEvent(this, args);
        }
        protected abstract void OnMessageEvent(object sender, WSMessageServerBaseEventArgs<Z> args);
        protected abstract void OnErrorEvent(object sender, WSErrorServerBaseEventArgs<Z> args);

        protected abstract X CreateWebsocketHandler(byte[] certificate = null, string certificatePassword = null);
        protected abstract Y CreateWSConnectionManager();

        protected virtual void FireEvent(object sender, ServerEventArgs args)
        {
            _serverEvent?.Invoke(sender, args);
        }

        public override void Dispose()
        {
            foreach (var connection in _connectionManager.GetAll())
            {
                DisconnectConnectionAsync(connection).Wait();
            }

            if (_handler != null)
            {
                _handler.ConnectionEvent -= OnConnectionEvent;
                _handler.MessageEvent -= OnMessageEvent;
                _handler.ErrorEvent -= OnErrorEvent;
                _handler.ServerEvent -= OnServerEvent;
                _handler.Dispose();
            }

            base.Dispose();
        }

        public TcpListener Server
        {
            get
            {
                return _handler?.Server;
            }
        }
        public bool IsServerRunning
        {
            get
            {
                return _handler != null && _handler.IsServerRunning;
            }
        }
        public IEnumerable<Z> Connections
        {
            get
            {
                return _connectionManager.GetAll();
            }
        }
        public int ConnectionCount
        {
            get
            {
                return _connectionManager.Count();
            }
        }

        public event NetworkingEventHandler<ServerEventArgs> ServerEvent
        {
            add
            {
                _serverEvent += value;
            }
            remove
            {
                _serverEvent -= value;
            }
        }
    }
}
