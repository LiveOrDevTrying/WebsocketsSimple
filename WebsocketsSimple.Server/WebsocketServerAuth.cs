using PHS.Networking.Enums;
using PHS.Networking.Events;
using PHS.Networking.Models;
using PHS.Networking.Server.Enums;
using PHS.Networking.Server.Events.Args;
using PHS.Networking.Server.Services;
using PHS.Networking.Services;
using System;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using WebsocketsSimple.Server.Events.Args;
using WebsocketsSimple.Server.Handlers;
using WebsocketsSimple.Server.Managers;
using WebsocketsSimple.Server.Models;

namespace WebsocketsSimple.Server
{
    public class WebsocketServerAuth<T> :
       CoreNetworking<WSConnectionServerAuthEventArgs<T>, WSMessageServerAuthEventArgs<T>, WSErrorServerAuthEventArgs<T>>, 
        IWebsocketServerAuth<T>
    {
        protected readonly WebsocketHandlerAuth _handler;
        protected readonly IParamsWSServerAuth _parameters;
        protected readonly IUserService<T> _userService;
        protected readonly WSConnectionManagerAuth<T> _connectionManager;
        protected Timer _timerPing;
        protected volatile bool _isPingRunning;
        protected const int PING_INTERVAL_SEC = 120;

        private event NetworkingEventHandler<ServerEventArgs> _serverEvent;

        public WebsocketServerAuth(IParamsWSServerAuth parameters,
            IUserService<T> userService,
            WebsocketHandlerAuth handler = null,
            WSConnectionManagerAuth<T> connectionManager = null)
        {
            _parameters = parameters;
            _userService = userService;
            _connectionManager = connectionManager ?? new WSConnectionManagerAuth<T>();

            _handler = handler ?? new WebsocketHandlerAuth(_parameters);
            _handler.ConnectionEvent += OnConnectionEventAsync;
            _handler.MessageEvent += OnMessageEventAsync;
            _handler.ErrorEvent += OnErrorEventAsync;
            _handler.ServerEvent += OnServerEventAsync;
            _handler.AuthorizeEvent += OnAuthorizeEventAsync;
        }

        public WebsocketServerAuth(IParamsWSServerAuth parameters,
            IUserService<T> userService,
            byte[] certificate,
            string certificatePassword,
            WebsocketHandlerAuth handler = null,
            WSConnectionManagerAuth<T> connectionManager = null)
        {
            _parameters = parameters;
            _userService = userService;
            _connectionManager = connectionManager ?? new WSConnectionManagerAuth<T>();

            _handler = handler ?? new WebsocketHandlerAuth(_parameters, certificate, certificatePassword);
            _handler.ConnectionEvent += OnConnectionEventAsync;
            _handler.MessageEvent += OnMessageEventAsync;
            _handler.ErrorEvent += OnErrorEventAsync;
            _handler.ServerEvent += OnServerEventAsync;
            _handler.AuthorizeEvent += OnAuthorizeEventAsync;
        }

        public virtual async Task StartAsync()
        {
            await _handler.StartAsync();
        }
        public virtual async Task StopAsync()
        {
            await _handler.StopAsync();
        }
        public virtual async Task BroadcastToAllAuthorizedUsersAsync<S>(S packet) where S : IPacket
        {
            if (_handler.IsServerRunning)
            {
                foreach (var identity in _connectionManager.GetAllIdentities())
                {
                    foreach (var connection in identity.Connections.ToList())
                    {
                        await SendToConnectionAsync(packet, connection);
                    }
                }
            }
        }
        public virtual async Task BroadcastToAllAuthorizedUsersAsync(string message)
        {
            await BroadcastToAllAuthorizedUsersAsync(new Packet
            {
                Data = message,
                Timestamp = DateTime.UtcNow
            });
        }
        public virtual async Task BroadcastToAllAuthorizedUsersAsync<S>(S packet, IConnectionWSServer connectionSending) where S : IPacket
        {
            if (_handler != null &&
                _handler.IsServerRunning)
            {
                foreach (var identity in _connectionManager.GetAllIdentities())
                {
                    foreach (var connection in identity.Connections.ToList())
                    {
                        if (connection.Client.GetHashCode() != connection.Client.GetHashCode())
                        {
                            await SendToConnectionAsync(packet, connection);
                        }
                    }
                }
            }
        }
        public virtual async Task BroadcastToAllAuthorizedUsersAsync(string message, IConnectionWSServer connectionSending)
        {
            await BroadcastToAllAuthorizedUsersAsync(new Packet
            {
                Data = message,
                Timestamp = DateTime.UtcNow
            }, connectionSending);
        }

        public virtual async Task BroadcastToAllAuthorizedUsersRawAsync(string message)
        {
            if (_handler != null &&
                _handler.IsServerRunning)
            {
                foreach (var identity in _connectionManager.GetAllIdentities())
                {
                    foreach (var connection in identity.Connections.ToList())
                    {
                        await SendToConnectionRawAsync(message, connection);
                    }
                }
            }
        }

        public virtual async Task SendToUserAsync<S>(S packet, T userId) where S : IPacket
        {
            if (_handler != null &&
                _handler.IsServerRunning &&
                _connectionManager.IsUserConnected(userId))
            {
                var user = _connectionManager.GetIdentity(userId);

                foreach (var connection in user.Connections.ToList())
                {
                    await SendToConnectionAsync(packet, connection);
                }
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
            if (_handler != null &&
                _handler.IsServerRunning &&
                _connectionManager.IsUserConnected(userId))
            {
                var user = _connectionManager.GetIdentity(userId);

                foreach (var connection in user.Connections)
                {
                    await SendToConnectionRawAsync(message, connection);
                }
            }
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

                        await FireEventAsync(this, new WSMessageServerAuthEventArgs<T>
                        {
                            Message = packet.Data,
                            MessageEventType = MessageEventType.Sent,
                            Connection = connection,
                            Packet = packet,
                            UserId = default
                        });
                        return true;
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
                            if (!await _handler.SendAsync(packet, connection))
                            {
                                return false;
                            }

                            await FireEventAsync(this, new WSMessageServerAuthEventArgs<T>
                            {
                                Message = packet.Data,
                                MessageEventType = MessageEventType.Sent,
                                Packet = packet,
                                UserId = identity.UserId,
                                Connection = connection
                            });

                            return true;
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

                            await DisconnectConnectionAsync(connection);

                            return false;
                        }
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

                        await FireEventAsync(this, new WSMessageServerAuthEventArgs<T>
                        {
                            Message = message,
                            MessageEventType = MessageEventType.Sent,
                            Packet = new Packet
                            {
                                Data = message,
                                Timestamp = DateTime.UtcNow
                            },
                            Connection = connection,
                            UserId = default
                        });

                        return true;
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
                            if (!await _handler.SendRawAsync(message, connection))
                            {
                                return false;
                            }

                            await FireEventAsync(this, new WSMessageServerAuthEventArgs<T>
                            {
                                Message = message,
                                Packet = new Packet
                                {
                                    Data = message,
                                    Timestamp = DateTime.UtcNow
                                },
                                UserId = identity.UserId,
                                Connection = connection,
                                MessageEventType = MessageEventType.Sent,
                            });

                            return true;
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

                            await DisconnectConnectionAsync(connection);
                        }
                    }
                }
            }

            return false;
        }
        public virtual async Task<bool> DisconnectConnectionAsync(IConnectionWSServer connection)
        {
            return await _handler.DisconnectConnectionAsync(connection);
        }

        protected virtual async Task<bool> CheckIfAuthorizedAsync(WSMessageServerEventArgs args)
        {
            try
            {
                // Check for token here
                if (_connectionManager.IsConnectionOpen(args.Connection))
                {
                    _connectionManager.RemoveConnection(args.Connection);

                    if (args.Message.Length < "oauth:".Length ||
                        !args.Message.ToLower().StartsWith("oauth:"))
                    {
                        await SendToConnectionRawAsync(_parameters.ConnectionUnauthorizedString, args.Connection);
                        await DisconnectConnectionAsync(args.Connection);

                        await FireEventAsync(this, new WSConnectionServerAuthEventArgs<T>
                        {
                            ConnectionEventType = ConnectionEventType.Disconnect,
                            Connection = args.Connection
                        });
                        return false;
                    }

                    var token = args.Message.Substring("oauth:".Length);


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

            try
            {
                await SendToConnectionRawAsync(_parameters.ConnectionUnauthorizedString, args.Connection);
                await DisconnectConnectionAsync(args.Connection);
            }
            catch
            { }

            await FireEventAsync(this, new WSConnectionServerAuthEventArgs<T>
            {
                ConnectionEventType = ConnectionEventType.Disconnect,
                Connection = args.Connection,
            });
            return false;
        }

        protected virtual async Task OnConnectionEventAsync(object sender, WSConnectionServerEventArgs args)
        {
            switch (args.ConnectionEventType)
            {
                case ConnectionEventType.Connected:
                    break;
                case ConnectionEventType.Disconnect:

                    if (_connectionManager.IsConnectionAuthorized(args.Connection))
                    {
                        var identity = _connectionManager.GetIdentity(args.Connection);
                        _connectionManager.RemoveIdentity(args.Connection);

                        if (identity != null)
                        {
                            await FireEventAsync(this, new WSConnectionServerAuthEventArgs<T>
                            {
                                Connection = args.Connection,
                                ConnectionEventType = args.ConnectionEventType,
                                UserId = identity.UserId,
                            });
                        }
                    }
                    break;
                case ConnectionEventType.Connecting:
                    await FireEventAsync(this, new WSConnectionServerAuthEventArgs<T>
                    {
                        ConnectionEventType = args.ConnectionEventType,
                        Connection = args.Connection,
                    });
                    break;
                default:
                    break;
            }
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

                    await FireEventAsync(this, args);
                    _timerPing = new Timer(OnTimerPingTick, null, PING_INTERVAL_SEC * 1000, PING_INTERVAL_SEC * 1000);
                    break;
                case ServerEventType.Stop:
                    if (_timerPing != null)
                    {
                        _timerPing.Dispose();
                        _timerPing = null;
                    }

                    await FireEventAsync(this, args);
                    break;
                default:
                    break;
            }
        }
        protected virtual async Task OnMessageEventAsync(object sender, WSMessageServerEventArgs args)
        {
            switch (args.MessageEventType)
            {
                case MessageEventType.Sent:
                    await FireEventAsync(this, new WSMessageServerAuthEventArgs<T>
                    {
                        Message = args.Packet.Data,
                        MessageEventType = MessageEventType.Sent,
                        Packet = args.Packet,
                        Connection = args.Connection,
                    });
                    break;
                case MessageEventType.Receive:
                    if (_connectionManager.IsConnectionAuthorized(args.Connection))
                    {
                        var identity = _connectionManager.GetIdentity(args.Connection);

                        if (identity != null)
                        {
                            await FireEventAsync(this, new WSMessageServerAuthEventArgs<T>
                            {
                                Message = args.Packet.Data,
                                MessageEventType = MessageEventType.Receive,
                                Packet = args.Packet,
                                UserId = identity.UserId,
                                Connection = args.Connection,
                            });
                        }
                    }
                    break;
                default:
                    break;
            }
        }
        protected virtual async Task OnErrorEventAsync(object sender, WSErrorServerEventArgs args)
        {
            if (_connectionManager.IsConnectionAuthorized(args.Connection))
            {
                var identity = _connectionManager.GetIdentity(args.Connection);

                if (identity != null)
                {
                    await FireEventAsync(this, new WSErrorServerAuthEventArgs<T>
                    {
                        Exception = args.Exception,
                        Message = args.Message,
                        UserId = identity.UserId,
                        Connection = args.Connection
                    });
                }
            }
            else
            {
                await FireEventAsync(this, new WSErrorServerAuthEventArgs<T>
                {
                    Exception = args.Exception,
                    Message = args.Message,
                    Connection = args.Connection
                });
            }
        }
        protected virtual async Task OnAuthorizeEventAsync(object sender, WSAuthorizeEventArgs args)
        {
            if (await _userService.IsValidTokenAsync(args.Token))
            {
                var userId = await _userService.GetIdAsync(args.Token);

                if (userId == null)
                {
                    await SendToConnectionRawAsync(_parameters.ConnectionUnauthorizedString, args.Connection);
                    await DisconnectConnectionAsync(args.Connection);

                    await FireEventAsync(this, new WSConnectionServerAuthEventArgs<T>
                    {
                        ConnectionEventType = ConnectionEventType.Disconnect,
                        Connection = args.Connection
                    });
                }

                if (await _handler.UpgradeConnectionCallbackAsync(args))
                {
                    var identity = _connectionManager.AddIdentity(userId, args.Connection);
                    await FireEventAsync(this, new WSConnectionServerAuthEventArgs<T>
                    {
                        ConnectionEventType = ConnectionEventType.Connected,
                        UserId = identity.UserId,
                        Connection = args.Connection,
                    });
                }
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

        protected virtual async Task FireEventAsync(object sender, ServerEventArgs args)
        {
            if (_serverEvent != null)
            {
                await _serverEvent?.Invoke(sender, args);
            }
        }

        public override void Dispose()
        {
            foreach (var item in _connectionManager.GetAllConnections())
            {
                try
                {
                    _connectionManager.RemoveConnection(item);
                }
                catch
                { }
            }

            foreach (var item in _connectionManager.GetAllIdentities())
            {
                foreach (var connection in item.Connections.ToList())
                {
                    try
                    {
                        DisconnectConnectionAsync(connection).Wait();
                    }
                    catch
                    { }
                }
            }

            if (_timerPing != null)
            {
                _timerPing.Dispose();
                _timerPing = null;
            }

            if (_handler != null)
            {
                _handler.ConnectionEvent -= OnConnectionEventAsync;
                _handler.MessageEvent -= OnMessageEventAsync;
                _handler.ErrorEvent -= OnErrorEventAsync;
                _handler.ServerEvent -= OnServerEventAsync;
                _handler.AuthorizeEvent -= OnAuthorizeEventAsync;


                _handler.Dispose();
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
        public IIdentityWS<T>[] Identities
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
