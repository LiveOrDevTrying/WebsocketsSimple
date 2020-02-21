using PHS.Tcp.Core.Async.Server.Models;
using System;
using System.Net.WebSockets;
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
using PHS.Networking.Server.Services;
using System.Linq;

namespace WebsocketsSimple.Server
{
    public class WebsocketServerAuth<T> :
        CoreNetworking<WSConnectionServerAuthEventArgs<T>, WSMessageServerAuthEventArgs<T>, WSErrorServerAuthEventArgs<T>>,
        IWebsocketServerAuth<T>
    {
        protected readonly WebsocketHandler _handler;
        protected readonly IParamsWSServerAuth _parameters;
        protected readonly IUserService<T> _userService;
        private readonly WebsocketConnectionManagerAuth<T> _connectionManager;
        private Timer _timerPing;
        private volatile bool _isPingRunning;

        private const int PING_INTERVAL_SEC = 120;

        private event NetworkingEventHandler<ServerEventArgs> _serverEvent;

        public WebsocketServerAuth(IParamsWSServerAuth parameters,
            IUserService<T> userService,
            WebsocketHandler handler = null)
        {
            _parameters = parameters;
            _userService = userService;
            _connectionManager = new WebsocketConnectionManagerAuth<T>();

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

        public virtual async Task BroadcastToAllUsersAsync<S>(S packet) where S : IPacket
        {
            foreach (var identity in _connectionManager.GetAllIdentities())
            {
                foreach (var connection in identity.Connections.ToList())
                {
                    if (connection.Websocket.State == WebSocketState.Open)
                    {
                        await SendToConnectionAsync(packet, connection);
                    }
                }
            }
        }
        public virtual async Task BroadcastToAllUsersAsync(string message)
        {
            await BroadcastToAllUsersAsync(new Packet
            {
                Data = message,
                Timestamp = DateTime.UtcNow
            });
        }
        public virtual async Task BroadcastToAllUsersAsync<S>(S packet, IConnectionServer connectionSending) where S : IPacket
        {
            foreach (var identity in _connectionManager.GetAllIdentities())
            {
                foreach (var connection in identity.Connections.ToList())
                {
                    if (connection.Websocket.State == WebSocketState.Open &&
                        connection.Websocket.GetHashCode() != connection.Websocket.GetHashCode())
                    {
                        await SendToConnectionAsync(packet, connection);
                    }
                }
            }
        }
        public virtual async Task BroadcastToAllUsersAsync(string message, IConnectionServer connectionSending)
        {
            await BroadcastToAllUsersAsync(new Packet
            {
                Data = message,
                Timestamp = DateTime.UtcNow
            }, connectionSending);
        }
        public virtual async Task BroadcastToAllUsersRawAsync(string message)
        {
            foreach (var identity in _connectionManager.GetAllIdentities())
            {
                foreach (var connection in identity.Connections.ToList())
                {
                    if (connection.Websocket.State == WebSocketState.Open)
                    {
                        await SendToConnectionAsync(message, connection);
                    }
                }
            }
        }
                
        public virtual async Task SendToUserAsync<S>(S packet, T userId) where S : IPacket
        {
            var identity = _connectionManager.GetIdentity(userId);

            foreach (var connection in identity.Connections.ToList())
            {
                await SendToConnectionAsync(packet, connection);
            }
        }
        public virtual async Task SendToUserAsync(string message, T userId)
        {
            await SendToUserAsync(new Packet
            {
                Data = message,
                Timestamp = DateTime.UtcNow
            }, userId);
        }
        public virtual async Task SendToUserRawAsync(string message, T userId)
        {
            var identity = _connectionManager.GetIdentity(userId);

            foreach (var connection in identity.Connections.ToList())
            {
                await SendToConnectionRawAsync(message, connection);
            }
        }
                
        public virtual async Task<bool> SendToConnectionAsync<S>(S packet, IConnectionServer connection) where S : IPacket
        {
            if (_connectionManager.IsConnectionOpen(connection))
            {
                try
                {
                    await _handler.SendAsync(packet, connection);

                    FireEvent(this, new WSMessageServerAuthEventArgs<T>
                    {
                        Connection = connection,
                        Message = packet.Data,
                        MessageEventType = MessageEventType.Sent,
                        Packet = packet,
                        UserId = default,
                    });

                    return true;

                }
                catch (Exception ex)
                {
                    FireEvent(this, new WSErrorServerAuthEventArgs<T>
                    {
                        Connection = connection,
                        UserId = default,
                        Exception = ex,
                        Message = ex.Message
                    });

                    await DisconnectConnectionAsync(connection);

                    return false;
                }
            }

            if (_connectionManager.IsConnectionAuthorized(connection))
            {
                var identity = _connectionManager.GetAllIdentities().FirstOrDefault(s => s.Connections.Any(t => t.ConnectionId == connection.ConnectionId));
                if (identity != null)
                {
                    try
                    {
                        await _handler.SendAsync(packet, connection);

                        FireEvent(this, new WSMessageServerAuthEventArgs<T>
                        {
                            Connection = connection,
                            Message = packet.Data,
                            MessageEventType = MessageEventType.Sent,
                            Packet = packet,
                            UserId = identity.Id,
                        });

                        return true;

                    }
                    catch (Exception ex)
                    {
                        FireEvent(this, new WSErrorServerAuthEventArgs<T>
                        {
                            Connection = connection,
                            UserId = identity.Id,
                            Exception = ex,
                            Message = ex.Message
                        });

                        await DisconnectConnectionAsync(connection);

                        return false;
                    }
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

                    FireEvent(this, new WSMessageServerAuthEventArgs<T>
                    {
                        Connection = connection,
                        Message = message,
                        MessageEventType = MessageEventType.Sent,
                        Packet = new Packet
                        {
                            Data = message,
                            Timestamp = DateTime.UtcNow
                        },
                        UserId = default,
                    });

                    return true;

                }
                catch (Exception ex)
                {
                    FireEvent(this, new WSErrorServerAuthEventArgs<T>
                    {
                        Connection = connection,
                        UserId = default,
                        Exception = ex,
                        Message = ex.Message
                    });

                    await DisconnectConnectionAsync(connection);

                    return false;
                }
            }

            if (_connectionManager.IsConnectionAuthorized(connection))
            {
                var identity = _connectionManager.GetAllIdentities().FirstOrDefault(s => s.Connections.Any(t => t.ConnectionId == connection.ConnectionId));
                if (identity != null)
                {
                    try
                    {
                        await _handler.SendRawAsync(message, connection);

                        FireEvent(this, new WSMessageServerAuthEventArgs<T>
                        {
                            Connection = connection,
                            Message = message,
                            MessageEventType = MessageEventType.Sent,
                            Packet = new Packet
                            {
                                Data = message,
                                Timestamp = DateTime.UtcNow
                            },
                            UserId = identity.Id,
                        });

                        return true;

                    }
                    catch (Exception ex)
                    {
                        FireEvent(this, new WSErrorServerAuthEventArgs<T>
                        {
                            Connection = connection,
                            UserId = identity.Id,
                            Exception = ex,
                            Message = ex.Message
                        });

                        await DisconnectConnectionAsync(connection);

                        return false;
                    }
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
                    FireEvent(this, new WSConnectionServerAuthEventArgs<T>
                    {
                        UserId = default,
                        Connection = connection,
                        ConnectionEventType = ConnectionEventType.Disconnect,
                    });
                }

                if (_connectionManager.IsConnectionAuthorized(connection))
                {
                    var identity = _connectionManager.GetIdentity(connection);
                    await _connectionManager.RemoveUserConnectionAsync(connection, true);

                    FireEvent(this, new WSConnectionServerAuthEventArgs<T>
                    {
                        UserId = identity.Id,
                        Connection = connection,
                        ConnectionEventType = ConnectionEventType.Disconnect,
                    });
                }

                _handler.DisconnectConnection(connection);
            }
            catch (Exception ex)
            {
                FireEvent(this, new WSErrorServerAuthEventArgs<T>
                {
                    Connection = connection,
                    Exception = ex,
                    Message = ex.Message,
                    UserId = default
                });
            }
        }
                
        public virtual async Task AuthorizeAndStartReceiving(IConnectionServer connection, string oauthToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(oauthToken))
                {
                    await SendToConnectionRawAsync(_parameters.ConnectionUnauthorizedString, connection);
                    FireEvent(this, new WSConnectionServerAuthEventArgs<T>
                    {
                        Connection = connection,
                        ConnectionEventType = ConnectionEventType.Disconnect,
                        UserId = default
                    });
                    return;
                }

                var userId = await _userService.GetIdAsync(oauthToken);

                if (userId == null)
                {
                    await SendToConnectionRawAsync(_parameters.ConnectionUnauthorizedString, connection);
                    FireEvent(this, new WSConnectionServerAuthEventArgs<T>
                    {
                        Connection = connection,
                        ConnectionEventType = ConnectionEventType.Disconnect,
                        UserId = default
                    });
                    return;
                }

                _connectionManager.AddUserConnection(userId, connection);
                await SendToConnectionRawAsync(_parameters.ConnectionSuccessString, connection);
                await _handler.StartReceiving(connection);

                FireEvent(this, new WSConnectionServerAuthEventArgs<T>
                {
                    Connection = connection,
                    ConnectionEventType = ConnectionEventType.Connected,
                    UserId = userId
                });
            }
            catch (Exception ex)
            {
                FireEvent(this, new WSErrorServerAuthEventArgs<T>
                {
                    Connection = connection,
                    Exception = ex,
                    Message = ex.Message,
                    UserId = default
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
                        FireEvent(this, new WSConnectionServerAuthEventArgs<T>
                        {
                            Connection = args.Connection,
                            ConnectionEventType = args.ConnectionEventType,
                            UserId = default
                        });
                        break;
                    case ConnectionEventType.Disconnect:
                        var identity = _connectionManager.GetIdentity(args.Connection);

                        if (identity != null)
                        {
                            await _connectionManager.RemoveUserConnectionAsync(args.Connection, true);
                            FireEvent(this, new WSConnectionServerAuthEventArgs<T>
                            {
                                Connection = args.Connection,
                                ConnectionEventType = args.ConnectionEventType,
                                UserId = identity.Id
                            });
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                FireEvent(this, new WSErrorServerAuthEventArgs<T>
                {
                    Connection = args.Connection,
                    Exception = ex,
                    Message = ex.Message,
                    UserId = default
                });
            }
        }
        private Task OnErrorEvent(object sender, WSErrorServerEventArgs args)
        {
            if (_connectionManager.IsConnectionAuthorized(args.Connection))
            {
                var identity = _connectionManager.GetIdentity(args.Connection);

                FireEvent(this, new WSErrorServerAuthEventArgs<T>
                {
                    Connection = args.Connection,
                    Exception = args.Exception,
                    Message = args.Message,
                    UserId = identity.Id
                });
            }
            return Task.CompletedTask;
        }
        private Task OnMessageEvent(object sender, WSMessageServerEventArgs args)
        {
            var identity = _connectionManager.GetIdentity(args.Connection);

            if (identity != null)
            {
                FireEvent(this, new WSMessageServerAuthEventArgs<T>
                {
                    Connection = args.Connection,
                    Message = args.Message,
                    MessageEventType = args.MessageEventType,
                    Packet = args.Packet,
                    UserId = identity.Id
                });
            }
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
                    foreach (var identity in _connectionManager.GetAllIdentities())
                    {
                        foreach (var connection in identity.Connections.ToList())
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
                                FireEvent(this, new WSErrorServerAuthEventArgs<T>
                                {
                                    Connection = connection,
                                    Exception = ex,
                                    Message = ex.Message,
                                    UserId = identity.Id
                                });
                            }
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
        public IUserConnections<T>[] UserConnections
        {
            get
            {
                return _connectionManager.GetAllIdentities();
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