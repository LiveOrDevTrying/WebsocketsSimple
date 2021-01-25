using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using WebsocketsSimple.Server.Events.Args;
using PHS.Networking.Services;
using WebsocketsSimple.Server.Handlers;
using WebsocketsSimple.Server.Managers;
using PHS.Networking.Models;
using WebsocketsSimple.Server.Models;
using PHS.Networking.Enums;
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
        protected readonly WebsocketConnectionManagerAuth<T> _connectionManager;
        protected Timer _timerPing;
        protected volatile bool _isPingRunning;
        protected const int PING_INTERVAL_SEC = 120;

        public WebsocketServerAuth(IParamsWSServerAuth parameters,
            IUserService<T> userService,
            WebsocketHandler handler = null,
            WebsocketConnectionManagerAuth<T> connectionManager = null)
        {
            _parameters = parameters;
            _userService = userService;
            _connectionManager = connectionManager ?? new WebsocketConnectionManagerAuth<T>();

            _handler = handler ?? new WebsocketHandler(parameters);
            _handler.ConnectionEvent += OnConnectionEvent;
            _handler.MessageEvent += OnMessageEvent;
            _handler.ErrorEvent += OnErrorEvent;

            _timerPing = new Timer(OnTimerPingTick, null, PING_INTERVAL_SEC * 1000, PING_INTERVAL_SEC * 1000);
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
        public virtual async Task BroadcastToAllUsersAsync<S>(S packet, IConnectionWSServer connectionSending) where S : IPacket
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
        public virtual async Task BroadcastToAllUsersAsync(string message, IConnectionWSServer connectionSending)
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
                
        public virtual async Task<bool> SendToConnectionAsync<S>(S packet, IConnectionWSServer connection) where S : IPacket
        {
            if (_connectionManager.IsConnectionOpen(connection))
            {
                try
                {
                    await _handler.SendAsync(packet, connection);

                    await FireEventAsync(this, new WSMessageServerAuthEventArgs<T>
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
                    await FireEventAsync(this, new WSErrorServerAuthEventArgs<T>
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

                        await FireEventAsync(this, new WSMessageServerAuthEventArgs<T>
                        {
                            Connection = connection,
                            Message = packet.Data,
                            MessageEventType = MessageEventType.Sent,
                            Packet = packet,
                            UserId = identity.UserId,
                        });

                        return true;

                    }
                    catch (Exception ex)
                    {
                        await FireEventAsync(this, new WSErrorServerAuthEventArgs<T>
                        {
                            Connection = connection,
                            UserId = identity.UserId,
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
            if (_connectionManager.IsConnectionOpen(connection))
            {
                try
                {
                    await _handler.SendRawAsync(message, connection);

                    await FireEventAsync(this, new WSMessageServerAuthEventArgs<T>
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
                    await FireEventAsync(this, new WSErrorServerAuthEventArgs<T>
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

                        await FireEventAsync(this, new WSMessageServerAuthEventArgs<T>
                        {
                            Connection = connection,
                            Message = message,
                            MessageEventType = MessageEventType.Sent,
                            Packet = new Packet
                            {
                                Data = message,
                                Timestamp = DateTime.UtcNow
                            },
                            UserId = identity.UserId,
                        });

                        return true;

                    }
                    catch (Exception ex)
                    {
                        await FireEventAsync(this, new WSErrorServerAuthEventArgs<T>
                        {
                            Connection = connection,
                            UserId = identity.UserId,
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
                
        public virtual async Task DisconnectConnectionAsync(IConnectionWSServer connection)
        {
            try
            {
                if (_connectionManager.IsConnectionOpen(connection))
                {
                    await _connectionManager.RemoveConnectionAsync(connection, true);
                    await FireEventAsync(this, new WSConnectionServerAuthEventArgs<T>
                    {
                        UserId = default,
                        Connection = connection,
                        ConnectionEventType = ConnectionEventType.Disconnect,
                    });
                }

                if (_connectionManager.IsConnectionAuthorized(connection))
                {
                    var identity = _connectionManager.GetIdentity(connection);
                    await _connectionManager.RemoveIdentityAsync(connection, true);

                    await FireEventAsync(this, new WSConnectionServerAuthEventArgs<T>
                    {
                        UserId = identity.UserId,
                        Connection = connection,
                        ConnectionEventType = ConnectionEventType.Disconnect,
                    });
                }

                await _handler.DisconnectConnectionAsync(connection);
            }
            catch (Exception ex)
            {
                await FireEventAsync(this, new WSErrorServerAuthEventArgs<T>
                {
                    Connection = connection,
                    Exception = ex,
                    Message = ex.Message,
                    UserId = default
                });
            }
        }
                
        public virtual async Task AuthorizeAndStartReceivingAsync(IConnectionWSServer connection, string oauthToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(oauthToken))
                {
                    await SendToConnectionRawAsync(_parameters.ConnectionUnauthorizedString, connection);
                    await FireEventAsync(this, new WSConnectionServerAuthEventArgs<T>
                    {
                        Connection = connection,
                        ConnectionEventType = ConnectionEventType.Disconnect,
                        UserId = default
                    });
                    return;
                }

                if (await _userService.IsValidTokenAsync(oauthToken))
                {
                    var userId = await _userService.GetIdAsync(oauthToken);

                    if (userId == null)
                    {
                        await SendToConnectionRawAsync(_parameters.ConnectionUnauthorizedString, connection);
                        await FireEventAsync(this, new WSConnectionServerAuthEventArgs<T>
                        {
                            Connection = connection,
                            ConnectionEventType = ConnectionEventType.Disconnect,
                            UserId = default
                        });
                        return;
                    }

                    _connectionManager.AddIdentity(userId, connection);
                    await SendToConnectionRawAsync(_parameters.ConnectionSuccessString, connection);

                    await FireEventAsync(this, new WSConnectionServerAuthEventArgs<T>
                    {
                        Connection = connection,
                        ConnectionEventType = ConnectionEventType.Connected,
                        UserId = userId
                    });

                    await _handler.StartReceivingAsync(connection);
                    return;
                }
            }
            catch (Exception ex)
            {
                await FireEventAsync(this, new WSErrorServerAuthEventArgs<T>
                {
                    Connection = connection,
                    Exception = ex,
                    Message = ex.Message,
                    UserId = default
                });
            }

            await SendToConnectionRawAsync(_parameters.ConnectionUnauthorizedString, connection);

            await FireEventAsync(this, new WSConnectionServerAuthEventArgs<T>
            {
                Connection = connection,
                ConnectionEventType = ConnectionEventType.Disconnect,
                UserId = default
            });

        }
        protected virtual async Task OnConnectionEvent(object sender, WSConnectionServerEventArgs args)
        {
            try
            {
                switch (args.ConnectionEventType)
                {
                    case ConnectionEventType.Connected:
                    case ConnectionEventType.Connecting:
                        await FireEventAsync(this, new WSConnectionServerAuthEventArgs<T>
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
                            await _connectionManager.RemoveIdentityAsync(args.Connection, true);
                            await FireEventAsync(this, new WSConnectionServerAuthEventArgs<T>
                            {
                                Connection = args.Connection,
                                ConnectionEventType = args.ConnectionEventType,
                                UserId = identity.UserId
                            });
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                await FireEventAsync(this, new WSErrorServerAuthEventArgs<T>
                {
                    Connection = args.Connection,
                    Exception = ex,
                    Message = ex.Message,
                    UserId = default
                });
            }
        }
        protected virtual async Task OnErrorEvent(object sender, WSErrorServerEventArgs args)
        {
            if (_connectionManager.IsConnectionAuthorized(args.Connection))
            {
                var identity = _connectionManager.GetIdentity(args.Connection);

                await FireEventAsync(this, new WSErrorServerAuthEventArgs<T>
                {
                    Connection = args.Connection,
                    Exception = args.Exception,
                    Message = args.Message,
                    UserId = identity.UserId
                });
            }
        }
        protected virtual async Task OnMessageEvent(object sender, WSMessageServerEventArgs args)
        {
            var identity = _connectionManager.GetIdentity(args.Connection);

            if (identity != null)
            {
                await FireEventAsync(this, new WSMessageServerAuthEventArgs<T>
                {
                    Connection = args.Connection,
                    Message = args.Message,
                    MessageEventType = args.MessageEventType,
                    Packet = args.Packet,
                    UserId = identity.UserId
                });
            }
        }
        protected virtual void OnTimerPingTick(object state)
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
                                await FireEventAsync(this, new WSErrorServerAuthEventArgs<T>
                                {
                                    Connection = connection,
                                    Exception = ex,
                                    Message = ex.Message,
                                    UserId = identity.UserId
                                });
                            }
                        }
                    }

                    _isPingRunning = false;
                });
            }
        }

        public override void Dispose()
        {
            if (_handler != null)
            {
                _handler.ConnectionEvent -= OnConnectionEvent;
                _handler.MessageEvent -= OnMessageEvent;
                _handler.ErrorEvent -= OnErrorEvent;
            }

            if (_timerPing != null)
            {
                _timerPing.Dispose();
            }

            base.Dispose();
        }

        public IConnectionWSServer[] Connections
        {
            get
            {
                return _connectionManager.GetAllConnections();
            }
        }
        public IUserConnectionsWS<T>[] UserConnections
        {
            get
            {
                return _connectionManager.GetAllIdentities();
            }
        }
    }
}