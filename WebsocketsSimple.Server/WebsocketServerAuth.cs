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
        protected readonly ParamsWSServerAuth _parameters;
        protected readonly IUserService<T> _userService;
        protected readonly WSConnectionManagerAuth<T> _connectionManager;
        protected Timer _timerPing;
        protected volatile bool _isPingRunning;
        protected const int PING_INTERVAL_SEC = 120;
        protected CancellationToken _cancellationToken;

        private event NetworkingEventHandler<ServerEventArgs> _serverEvent;

        public WebsocketServerAuth(ParamsWSServerAuth parameters,
            IUserService<T> userService,
            WebsocketHandlerAuth handler = null,
            WSConnectionManagerAuth<T> connectionManager = null)
        {
            _parameters = parameters;
            _userService = userService;
            _connectionManager = connectionManager ?? new WSConnectionManagerAuth<T>();

            _handler = handler ?? new WebsocketHandlerAuth(_parameters);
            _handler.ConnectionEvent += OnConnectionEvent;
            _handler.MessageEvent += OnMessageEvent;
            _handler.ErrorEvent += OnErrorEvent;
            _handler.ServerEvent += OnServerEvent;
            _handler.AuthorizeEvent += OnAuthorizeEvent;
        }

        public WebsocketServerAuth(ParamsWSServerAuth parameters,
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
            _handler.ConnectionEvent += OnConnectionEvent;
            _handler.MessageEvent += OnMessageEvent;
            _handler.ErrorEvent += OnErrorEvent;
            _handler.ServerEvent += OnServerEvent;
            _handler.AuthorizeEvent += OnAuthorizeEvent;
        }

        public virtual void Start(CancellationToken cancellationToken = default)
        {
            _cancellationToken = cancellationToken;
            _handler.Start(_cancellationToken);
        }
        public virtual void Stop()
        {
            _handler.Stop();
        }

        public virtual async Task BroadcastToAllAuthorizedUsersAsync(string message, IConnectionWSServer connectionSending = null, CancellationToken cancellationToken = default)
        {
            if (_handler != null &&
                _handler.IsServerRunning)
            {
                foreach (var identity in _connectionManager.GetAllIdentities())
                {
                    foreach (var connection in identity.Connections.ToList())
                    {
                        if (connectionSending == null || connection.Client.GetHashCode() != connectionSending.Client.GetHashCode())
                        {
                            await SendToConnectionAsync(message, connection, cancellationToken);
                        }
                    }
                }
            }
        }
        public virtual async Task BroadcastToAllAuthorizedUsersAsync(byte[] message, IConnectionWSServer connectionSending = null, CancellationToken cancellationToken = default)
        {
            if (_handler != null &&
                _handler.IsServerRunning)
            {
                foreach (var identity in _connectionManager.GetAllIdentities())
                {
                    foreach (var connection in identity.Connections.ToList())
                    {
                        if (connectionSending == null || connection.Client.GetHashCode() != connectionSending.Client.GetHashCode())
                        {
                            await SendToConnectionAsync(message, connection, cancellationToken);
                        }
                    }
                }
            }
        }

        public virtual async Task SendToUserAsync(string message, T userId, IConnectionWSServer connectionSending = null, CancellationToken cancellationToken = default)
        {
            if (_handler != null &&
                _handler.IsServerRunning &&
                _connectionManager.IsUserConnected(userId))
            {
                var user = _connectionManager.GetIdentity(userId);

                foreach (var connection in user.Connections.Where(x => x.Client.GetHashCode() != connectionSending.GetHashCode()))
                {
                    await SendToConnectionAsync(message, connection, cancellationToken);
                }
            }
        }
        public virtual async Task SendToUserAsync(byte[] message, T userId, IConnectionWSServer connectionSending = null, CancellationToken cancellationToken = default)
        {
            if (_handler != null &&
                _handler.IsServerRunning &&
                _connectionManager.IsUserConnected(userId))
            {
                var user = _connectionManager.GetIdentity(userId);

                foreach (var connection in user.Connections.Where(x => x.Client.GetHashCode() != connectionSending.GetHashCode()))
                {
                    await SendToConnectionAsync(message, connection, cancellationToken);
                }
            }
        }

        public virtual async Task<bool> SendToConnectionAsync(string message, IConnectionWSServer connection, CancellationToken cancellationToken = default)
        {
            if (_handler.IsServerRunning)
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
                        FireEvent(this, new WSErrorServerAuthEventArgs<T>
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
                            if (!await _handler.SendAsync(message, connection, cancellationToken))
                            {
                                return false;
                            }

                            return true;
                        }
                        catch (Exception ex)
                        {
                            FireEvent(this, new WSErrorServerAuthEventArgs<T>
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
                        FireEvent(this, new WSErrorServerAuthEventArgs<T>
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
                            if (!await _handler.SendAsync(message, connection, cancellationToken))
                            {
                                return false;
                            }

                            return true;
                        }
                        catch (Exception ex)
                        {
                            FireEvent(this, new WSErrorServerAuthEventArgs<T>
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

        public virtual async Task<bool> DisconnectConnectionAsync(IConnectionWSServer connection, CancellationToken cancellationToken = default)
        {
            return await _handler.DisconnectConnectionAsync(connection, cancellationToken);
        }

        //protected virtual async Task<bool> CheckIfAuthorizedAsync(WSMessageServerEventArgs args)
        //{
        //    try
        //    {
        //        // Check for token here
        //        if (_connectionManager.IsConnectionOpen(args.Connection))
        //        {
        //            _connectionManager.RemoveConnection(args.Connection);

        //            if (args.Message.Length < "oauth:".Length ||
        //                !args.Packet.Data.ToLower().StartsWith("oauth:"))
        //            {
        //                await SendToConnectionRawAsync(_parameters.ConnectionUnauthorizedString, args.Connection);
        //                await DisconnectConnectionAsync(args.Connection);

        //                FireEvent(this, new WSConnectionServerAuthEventArgs<T>
        //                {
        //                    ConnectionEventType = ConnectionEventType.Disconnect,
        //                    Connection = args.Connection
        //                });
        //                return false;
        //            }

        //            var token = args.Packet.Data.Substring("oauth:".Length);


        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        FireEvent(this, new WSErrorServerAuthEventArgs<T>
        //        {
        //            Connection = args.Connection,
        //            Exception = ex,
        //            Message = ex.Message,
        //            UserId = default
        //        });
        //    }

        //    try
        //    {
        //        await SendToConnectionRawAsync(_parameters.ConnectionUnauthorizedString, args.Connection);
        //        await DisconnectConnectionAsync(args.Connection);
        //    }
        //    catch
        //    { }

        //    FireEvent(this, new WSConnectionServerAuthEventArgs<T>
        //    {
        //        ConnectionEventType = ConnectionEventType.Disconnect,
        //        Connection = args.Connection,
        //    });
        //    return false;
        //}

        protected virtual void OnConnectionEvent(object sender, WSConnectionServerEventArgs args)
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
                            FireEvent(this, new WSConnectionServerAuthEventArgs<T>
                            {
                                Connection = args.Connection,
                                ConnectionEventType = args.ConnectionEventType,
                                UserId = identity.UserId,
                            });
                        }
                    }
                    break;
                case ConnectionEventType.Connecting:
                    FireEvent(this, new WSConnectionServerAuthEventArgs<T>
                    {
                        ConnectionEventType = args.ConnectionEventType,
                        Connection = args.Connection,
                    });
                    break;
                default:
                    break;
            }
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

                    FireEvent(this, args);
                    _timerPing = new Timer(OnTimerPingTick, null, PING_INTERVAL_SEC * 1000, PING_INTERVAL_SEC * 1000);
                    break;
                case ServerEventType.Stop:
                    if (_timerPing != null)
                    {
                        _timerPing.Dispose();
                        _timerPing = null;
                    }

                    FireEvent(this, args);
                    break;
                default:
                    break;
            }
        }
        protected virtual void OnMessageEvent(object sender, WSMessageServerEventArgs args)
        {
            switch (args.MessageEventType)
            {
                case MessageEventType.Sent:
                    if (_connectionManager.IsConnectionAuthorized(args.Connection))
                    {
                        var identity = _connectionManager.GetIdentity(args.Connection);

                        if (identity != null)
                        {
                            FireEvent(this, new WSMessageServerAuthEventArgs<T>
                            {
                                MessageEventType = MessageEventType.Sent,
                                Message = args.Message,
                                Bytes = args.Bytes,
                                Connection = args.Connection,
                                UserId = identity.UserId
                            });
                        }
                    }
                    else
                    {
                        FireEvent(this, new WSMessageServerAuthEventArgs<T>
                        {
                            MessageEventType = MessageEventType.Sent,
                            Message = args.Message,
                            Bytes = args.Bytes,
                            Connection = args.Connection,
                        });
                    }
                    break;
                case MessageEventType.Receive:
                    if (_connectionManager.IsConnectionAuthorized(args.Connection))
                    {
                        var identity = _connectionManager.GetIdentity(args.Connection);

                        if (identity != null)
                        {
                            FireEvent(this, new WSMessageServerAuthEventArgs<T>
                            {
                                MessageEventType = MessageEventType.Receive,
                                Message = args.Message,
                                Bytes = args.Bytes,
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
        protected virtual void OnErrorEvent(object sender, WSErrorServerEventArgs args)
        {
            if (_connectionManager.IsConnectionAuthorized(args.Connection))
            {
                var identity = _connectionManager.GetIdentity(args.Connection);

                if (identity != null)
                {
                    FireEvent(this, new WSErrorServerAuthEventArgs<T>
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
                FireEvent(this, new WSErrorServerAuthEventArgs<T>
                {
                    Exception = args.Exception,
                    Message = args.Message,
                    Connection = args.Connection
                });
            }
        }
        protected virtual void OnAuthorizeEvent(object sender, WSAuthorizeEventArgs args)
        {
            Task.Run(async () =>
            {
                if (await _userService.IsValidTokenAsync(args.Token, _cancellationToken))
                {
                    var userId = await _userService.GetIdAsync(args.Token, _cancellationToken);

                    if (userId != null &&
                        await _handler.UpgradeConnectionCallbackAsync(args, _cancellationToken))
                    {
                        var identity = _connectionManager.AddIdentity(userId, args.Connection);
                        FireEvent(this, new WSConnectionServerAuthEventArgs<T>
                        {
                            ConnectionEventType = ConnectionEventType.Connected,
                            UserId = identity.UserId,
                            Connection = args.Connection,
                        });
                        return;
                    }
                }

                await SendToConnectionAsync(_parameters.ConnectionUnauthorizedString, args.Connection, _cancellationToken);
                await DisconnectConnectionAsync(args.Connection, _cancellationToken);

                FireEvent(this, new WSConnectionServerAuthEventArgs<T>
                {
                    ConnectionEventType = ConnectionEventType.Disconnect,
                    Connection = args.Connection
                });
            }, _cancellationToken);
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
                                FireEvent(this, new WSErrorServerAuthEventArgs<T>
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
                }, _cancellationToken);
            }
        }

        protected virtual void FireEvent(object sender, ServerEventArgs args)
        {
            _serverEvent?.Invoke(sender, args);
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
                _handler.ConnectionEvent -= OnConnectionEvent;
                _handler.MessageEvent -= OnMessageEvent;
                _handler.ErrorEvent -= OnErrorEvent;
                _handler.ServerEvent -= OnServerEvent;
                _handler.AuthorizeEvent -= OnAuthorizeEvent;


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
