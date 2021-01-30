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
        protected readonly IParamsWSServer _parameters;
        protected readonly WSConnectionManager _connectionManager;
        protected Timer _timerPing;
        protected volatile bool _isPingRunning;
        protected const int PING_INTERVAL_SEC = 120;
        
        private event NetworkingEventHandler<ServerEventArgs> _serverEvent;

        public WebsocketServer(IParamsWSServer parameters, 
            WebsocketHandler handler = null, 
            WSConnectionManager connectionManager = null)
        {
            _parameters = parameters;
            _connectionManager = connectionManager ?? new WSConnectionManager();

            _handler = handler ?? new WebsocketHandler(_parameters);
            _handler.ConnectionEvent += OnConnectionEventAsync;
            _handler.MessageEvent += OnMessageEventAsync;
            _handler.ErrorEvent += OnErrorEventAsync;
            _handler.ServerEvent += OnServerEventAsync;
        }
        public WebsocketServer(IParamsWSServer parameters,
            byte[] certificate,
            string certificatePassword,
            WebsocketHandler handler = null,
            WSConnectionManager connectionManager = null)
        {
            _parameters = parameters;
            _connectionManager = connectionManager ?? new WSConnectionManager();

            _handler = handler ?? new WebsocketHandler(_parameters, certificate, certificatePassword);
            _handler.ConnectionEvent += OnConnectionEventAsync;
            _handler.MessageEvent += OnMessageEventAsync;
            _handler.ErrorEvent += OnErrorEventAsync;
            _handler.ServerEvent += OnServerEventAsync;
        }

        public virtual async Task StartAsync()
        {
            await _handler.StartAsync();
        }
        public virtual async Task StopAsync()
        {
            await _handler.StopAsync();
        }

        public virtual async Task<bool> SendToConnectionAsync<S>(S packet, IConnectionWSServer connection) where S : IPacket
        {
            if (_handler.IsServerRunning)
            {
                if (_connectionManager.IsConnectionOpen(connection))
                {
                    try
                    {
                        if (!await _handler.SendAsync(packet, connection))
                        {
                            return false;
                        }

                        await FireEventAsync(this, new WSMessageServerEventArgs
                        {
                            Message = packet.Data,
                            MessageEventType = MessageEventType.Sent,
                            Connection = connection,
                            Packet = packet,
                        });
                        return true;
                    }
                    catch (Exception ex)
                    {
                        await FireEventAsync(this, new WSErrorServerEventArgs
                        {
                            Connection = connection,
                            Exception = ex,
                            Message = ex.Message,
                        });

                        await DisconnectConnectionAsync(connection);

                        return false;
                    }
                }
            }

            return false;
        }
    
        public virtual async Task<bool> SendToConnectionAsync(string message, IConnectionWSServer connection)
        {
            return await SendToConnectionAsync(new Packet
            {
                Data = message,
                Timestamp = DateTime.UtcNow
            }, connection);
        }
        public virtual async Task<bool> SendToConnectionRawAsync(string message, IConnectionWSServer connection)
        {
            if (_handler != null &&
                _handler.IsServerRunning)
            {
                if (_connectionManager.IsConnectionOpen(connection))
                {
                    try
                    {
                        if (!await _handler.SendRawAsync(message, connection))
                        {
                            return false;
                        }

                        await FireEventAsync(this, new WSMessageServerEventArgs
                        {
                            Message = message,
                            MessageEventType = MessageEventType.Sent,
                            Packet = new Packet
                            {
                                Data = message,
                                Timestamp = DateTime.UtcNow
                            },
                            Connection = connection,
                        });

                        return true;
                    }
                    catch (Exception ex)
                    {
                        await FireEventAsync(this, new WSErrorServerEventArgs
                        {
                            Connection = connection,
                            Exception = ex,
                            Message = ex.Message,
                        });

                        await DisconnectConnectionAsync(connection);

                    }
                }
            }
            
            return false;
        }

        public virtual async Task<bool> DisconnectConnectionAsync(IConnectionWSServer connection)
        {
            return await _handler.DisconnectConnectionAsync(connection);
        }

        protected virtual async Task OnConnectionEventAsync(object sender, WSConnectionServerEventArgs args)
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

            await FireEventAsync(this, args);
        }
        protected virtual async Task OnServerEventAsync(object sender, ServerEventArgs args)
        {
            switch (args.ServerEventType)
            {
                case ServerEventType.Start:
                    if (_timerPing != null)
                    {
                        _timerPing.Dispose();
                        _timerPing = null;
                    }

                    await FireEventAsync(sender, args);

                    _timerPing = new Timer(OnTimerPingTick, null, PING_INTERVAL_SEC * 1000, PING_INTERVAL_SEC * 1000);
                    break;
                case ServerEventType.Stop:
                    if (_timerPing != null)
                    {
                        _timerPing.Dispose();
                        _timerPing = null;
                    }

                    await FireEventAsync(sender, args);
                    break;
                default:
                    break;
            }
        }
        protected virtual async Task OnMessageEventAsync(object sender, WSMessageServerEventArgs args)
        {
            await FireEventAsync(sender, args);
        }
        protected virtual async Task OnErrorEventAsync(object sender, WSErrorServerEventArgs args)
        {
            await FireEventAsync(this, args);
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
                                await SendToConnectionRawAsync("No ping response - disconnected.", connection);
                                await DisconnectConnectionAsync(connection);
                            }
                            else
                            {
                                connection.HasBeenPinged = true;
                                await SendToConnectionRawAsync("Ping", connection);
                            }
                        }
                        catch (Exception ex)
                        {
                            await FireEventAsync(this, new WSErrorServerEventArgs
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

        protected virtual async Task FireEventAsync(object sender, ServerEventArgs args)
        {
            if (_serverEvent != null)
            {
                await _serverEvent?.Invoke(sender, args);
            }
        }

        public override void Dispose()
        {
            foreach (var connection in _connectionManager.GetAllConnections())
            {
                DisconnectConnectionAsync(connection).Wait();
            }

            if (_handler != null)
            {
                _handler.ConnectionEvent -= OnConnectionEventAsync;
                _handler.MessageEvent -= OnMessageEventAsync;
                _handler.ErrorEvent -= OnErrorEventAsync;
                _handler.ServerEvent -= OnServerEventAsync;
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
