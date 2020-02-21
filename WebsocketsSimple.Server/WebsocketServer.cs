using PHS.Tcp.Core.Async.Server.Models;
using System;
using System.Threading;
using System.Threading.Tasks;
using WebsocketsSimple.Server.Events.Args;
using PHS.Networking.Services;
using WebsocketsSimple.Server.Handlers;
using WebsocketsSimple.Server.Managers;
using PHS.Networking.Models;
using PHS.Networking.Events;
using PHS.Networking.Server.Events.Args;
using WebsocketsSimple.Server.Models;
using PHS.Networking.Enums;
using PHS.Networking.Server.Enums;

namespace WebsocketsSimple.Server
{
    public class WebsocketServer :
        CoreNetworking<WSConnectionServerEventArgs, WSMessageServerEventArgs, WSErrorServerEventArgs>,
        IWebsocketServer
    {
        protected readonly WebsocketHandler _handler;
        protected readonly IParamsWSServer _parameters;
        private readonly WebsocketConnectionManager _connectionManager;
        private Timer _timerPing;
        private volatile bool _isPingRunning;

        private const int PING_INTERVAL_SEC = 120;

        private event NetworkingEventHandler<ServerEventArgs> _serverEvent;

        public WebsocketServer(IParamsWSServer parameters,
            WebsocketHandler handler = null)
        {
            _parameters = parameters;
            _connectionManager = new WebsocketConnectionManager();

            _handler = handler ?? new WebsocketHandler(parameters);
            _handler.ConnectionEvent += OnConnectionEvent;
            _handler.MessageEvent += OnMessageEvent;
            _handler.ErrorEvent += OnErrorEvent;
            _handler.ServerEvent += OnServerEvent;

            _timerPing = new Timer(OnTimerPingTick, null, PING_INTERVAL_SEC * 1000, PING_INTERVAL_SEC * 1000);

            FireEvent(this, new ServerEventArgs
            {
                ServerEventType = ServerEventType.Start
            });
        }

        public virtual async Task<bool> SendToConnectionAsync<S>(S packet, IConnectionServer connection) where S : IPacket
        {
            if (_connectionManager.IsConnectionOpen(connection))
            {
                try
                {
                    await _handler.SendAsync(packet, connection);

                    FireEvent(this, new WSMessageServerEventArgs
                    {
                        Connection = connection,
                        Message = packet.Data,
                        MessageEventType = MessageEventType.Sent,
                        Packet = packet,
                    });

                    return true;

                }
                catch (Exception ex)
                {
                    FireEvent(this, new WSErrorServerEventArgs
                    {
                        Connection = connection,
                        Exception = ex,
                        Message = ex.Message
                    });

                    await DisconnectConnectionAsync(connection);

                    return false;
                }
            }
           
            return false;
        }
        public virtual async Task<bool> SendToConnectionAsync(string message, IConnectionServer connection)
        {
            return await SendToConnectionAsync(new Packet
            {
                Data = message,
                Timestamp = DateTime.UtcNow
            }, connection);
        }
        public virtual async Task<bool> SendToConnectionRawAsync(string message, IConnectionServer connection)
        {
            if (_connectionManager.IsConnectionOpen(connection))
            {
                try
                {
                    await _handler.SendRawAsync(message, connection);

                    FireEvent(this, new WSMessageServerEventArgs
                    {
                        Connection = connection,
                        Message = message,
                        MessageEventType = MessageEventType.Sent,
                        Packet = new Packet
                        {
                            Data = message,
                            Timestamp = DateTime.UtcNow
                        },
                    });

                    return true;

                }
                catch (Exception ex)
                {
                    FireEvent(this, new WSErrorServerEventArgs
                    {
                        Connection = connection,
                        Exception = ex,
                        Message = ex.Message
                    });

                    await DisconnectConnectionAsync(connection);

                    return false;
                }
            }

            return false;
        }
                
        public virtual async Task DisconnectConnectionAsync(IConnectionServer connection)
        {
            try
            {
                if (_connectionManager.IsConnectionOpen(connection))
                {
                    await _connectionManager.RemoveConnectionAsync(connection, true);
                    
                    _handler.DisconnectConnection(connection);

                    FireEvent(this, new WSConnectionServerEventArgs
                    {
                        Connection = connection,
                        ConnectionEventType = ConnectionEventType.Disconnect,
                    });
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
                
        public virtual async Task StartReceiving(IConnectionServer connection)
        {
            try
            {
                _connectionManager.AddConnection(connection);
                await SendToConnectionRawAsync(_parameters.ConnectionSuccessString, connection);
                await _handler.StartReceiving(connection);

                FireEvent(this, new WSConnectionServerEventArgs
                {
                    Connection = connection,
                    ConnectionEventType = ConnectionEventType.Connected,
                });
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

        private async Task OnConnectionEvent(object sender, WSConnectionServerEventArgs args)
        {
            try
            {
                switch (args.ConnectionEventType)
                {
                    case ConnectionEventType.Connected:
                    case ConnectionEventType.Connecting:
                        FireEvent(this, new WSConnectionServerEventArgs
                        {
                            Connection = args.Connection,
                            ConnectionEventType = args.ConnectionEventType,
                        });
                        break;
                    case ConnectionEventType.Disconnect:
                        await _connectionManager.RemoveConnectionAsync(args.Connection, true);
                        FireEvent(this, new WSConnectionServerEventArgs
                        {
                            Connection = args.Connection,
                            ConnectionEventType = args.ConnectionEventType,
                        });
                        break;
                }
            }
            catch (Exception ex)
            {
                FireEvent(this, new WSErrorServerEventArgs
                {
                    Connection = args.Connection,
                    Exception = ex,
                    Message = ex.Message,
                });
            }
        }
        private Task OnErrorEvent(object sender, WSErrorServerEventArgs args)
        {
            FireEvent(sender, args);
            return Task.CompletedTask;
        }
        private Task OnMessageEvent(object sender, WSMessageServerEventArgs args)
        {
            FireEvent(sender, args);
            return Task.CompletedTask;
        }
        private Task OnServerEvent(object sender, ServerEventArgs args)
        {
            FireEvent(sender, args);
            return Task.CompletedTask;
        }
        private void OnTimerPingTick(object state)
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

        protected void FireEvent(object sender, ServerEventArgs args)
        {
            _serverEvent?.Invoke(sender, args);
        }

        public override void Dispose()
        {
            if (_handler != null)
            {
                _handler.ConnectionEvent -= OnConnectionEvent;
                _handler.MessageEvent -= OnMessageEvent;
                _handler.ErrorEvent -= OnErrorEvent;
                _handler.ServerEvent -= OnServerEvent;
            }

            if (_timerPing != null)
            {
                _timerPing.Dispose();
            }

            FireEvent(this, new ServerEventArgs
            {
                ServerEventType = ServerEventType.Stop
            });

            base.Dispose();
        }

        public IConnectionServer[] Connections
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