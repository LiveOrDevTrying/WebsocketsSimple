using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using PHS.Networking.Models;
using PHS.Networking.Server.Events.Args;
using PHS.Networking.Services;
using PHS.Networking.Events;
using PHS.Networking.Server.Enums;
using PHS.Networking.Enums;
using WebsocketsSimple.Server.Events.Args;
using WebsocketsSimple.Server.Handlers;
using WebsocketsSimple.Server.Models;
using WebsocketsSimple.Server.Managers;

namespace WebsocketsSimple.Server
{
    public class WebsocketServer : 
        CoreNetworking<WSConnectionServerEventArgs, WSMessageServerEventArgs, WSErrorServerEventArgs>, 
        IWebsocketServer
    {
        protected readonly WebsocketHandler _handler;
        protected readonly ParamsWSServer _parameters;
        protected readonly WSConnectionManager _connectionManager;
        protected Timer _timerPing;
        protected volatile bool _isPingRunning;
        protected const int PING_INTERVAL_SEC = 120;
        protected CancellationToken _cancellationToken;

        private event NetworkingEventHandler<ServerEventArgs> _serverEvent;

        public WebsocketServer(ParamsWSServer parameters, 
            WebsocketHandler handler = null, 
            WSConnectionManager connectionManager = null)
        {
            _parameters = parameters;
            _connectionManager = connectionManager ?? new WSConnectionManager();

            _handler = handler ?? new WebsocketHandler(_parameters);
            _handler.ConnectionEvent += OnConnectionEvent;
            _handler.MessageEvent += OnMessageEvent;
            _handler.ErrorEvent += OnErrorEvent;
            _handler.ServerEvent += OnServerEvent;
        }
        public WebsocketServer(ParamsWSServer parameters,
            byte[] certificate,
            string certificatePassword,
            WebsocketHandler handler = null,
            WSConnectionManager connectionManager = null)
        {
            _parameters = parameters;
            _connectionManager = connectionManager ?? new WSConnectionManager();

            _handler = handler ?? new WebsocketHandler(_parameters, certificate, certificatePassword);
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

        public virtual async Task<bool> SendToConnectionAsync(string message, IConnectionWSServer connection, CancellationToken cancellationToken = default)
        {
            if (_handler != null &&
                _handler.IsServerRunning)
            {
                if (_connectionManager.IsConnectionOpen(connection))
                {
                    try
                    {
                        if (!await _handler.SendAsync(message, connection, cancellationToken))
                        {
                            return false;
                        }

                        return true;
                    }
                    catch (Exception ex)
                    {
                        FireEvent(this, new WSErrorServerEventArgs
                        {
                            Connection = connection,
                            Exception = ex,
                            Message = ex.Message,
                        });

                        await DisconnectConnectionAsync(connection, cancellationToken);

                    }
                }
            }
            
            return false;
        }
        public virtual async Task<bool> SendToConnectionAsync(byte[] message, IConnectionWSServer connection, CancellationToken cancellationToken = default)
        {
            if (_handler != null &&
                _handler.IsServerRunning)
            {
                if (_connectionManager.IsConnectionOpen(connection))
                {
                    try
                    {
                        if (!await _handler.SendAsync(message, connection, cancellationToken))
                        {
                            return false;
                        }

                        return true;
                    }
                    catch (Exception ex)
                    {
                        FireEvent(this, new WSErrorServerEventArgs
                        {
                            Connection = connection,
                            Exception = ex,
                            Message = ex.Message,
                        });

                        await DisconnectConnectionAsync(connection, cancellationToken);

                    }
                }
            }
            
            return false;
        }

        public virtual async Task<bool> DisconnectConnectionAsync(IConnectionWSServer connection, CancellationToken cancellationToken = default)
        {
            return await _handler.DisconnectConnectionAsync(connection, cancellationToken);
        }

        protected virtual void OnConnectionEvent(object sender, WSConnectionServerEventArgs args)
        {
            switch (args.ConnectionEventType)
            {
                case ConnectionEventType.Connected:
                    _connectionManager.AddConnection(args.Connection);
                    break;
                case ConnectionEventType.Disconnect:
                    _connectionManager.RemoveConnection(args.Connection);
                    break;
                case ConnectionEventType.Connecting:
                    break;
                default:
                    break;
            }

            FireEvent(this, args);
        }
        protected virtual void OnServerEvent(object sender, ServerEventArgs args)
        {
            switch (args.ServerEventType)
            {
                case ServerEventType.Start:
                    if (_timerPing != null)
                    {
                        _timerPing.Dispose();
                        _timerPing = null;
                    }

                    _timerPing = new Timer(OnTimerPingTick, null, PING_INTERVAL_SEC * 1000, PING_INTERVAL_SEC * 1000);
                    break;
                case ServerEventType.Stop:
                    if (_timerPing != null)
                    {
                        _timerPing.Dispose();
                        _timerPing = null;
                    }
                    break;
                default:
                    break;
            }

            FireEvent(sender, args);
        }
        protected virtual void OnMessageEvent(object sender, WSMessageServerEventArgs args)
        {
            FireEvent(sender, args);
        }
        protected virtual void OnErrorEvent(object sender, WSErrorServerEventArgs args)
        {
            FireEvent(this, args);
        }
        
        protected virtual void OnTimerPingTick(object state)
        {
            if (!_isPingRunning)
            {
                _isPingRunning = true;

                Task.Run(async () =>
                {
                    foreach (var connection in _connectionManager.GetAllConnections())
                    {
                        try
                        {
                            if (connection.HasBeenPinged)
                            {
                                // Already been pinged, no response, disconnect
                                await SendToConnectionAsync("No ping response - disconnected.", connection, _cancellationToken);
                                await DisconnectConnectionAsync(connection, _cancellationToken);
                            }
                            else
                            {
                                connection.HasBeenPinged = true;
                                await SendToConnectionAsync("Ping", connection, _cancellationToken);
                            }
                        }
                        catch (Exception ex)
                        {
                            FireEvent(this, new WSErrorServerEventArgs
                            {
                                Connection = connection,
                                Exception = ex,
                                Message = ex.Message,
                            });
                        }
                    }

                    _isPingRunning = false;
                });
            }
        }

        protected virtual void FireEvent(object sender, ServerEventArgs args)
        {
            _serverEvent?.Invoke(sender, args);
        }

        public override void Dispose()
        {
            foreach (var connection in _connectionManager.GetAllConnections())
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

            if (_timerPing != null)
            {
                _timerPing.Dispose();
                _timerPing = null;
            }
            base.Dispose();
        }

        public TcpListener Server
        {
            get
            {
                return _handler != null ? _handler.Server : null;
            }
        }
        public bool IsServerRunning
        {
            get
            {
                return _handler != null ? _handler.IsServerRunning : false;
            }
        }
        public IConnectionWSServer[] Connections
        {
            get
            {
                return _connectionManager.GetAllConnections();
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
