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
        WebsocketServerBase<
            WSConnectionServerAuthEventArgs<T>, 
            WSMessageServerAuthEventArgs<T>, 
            WSErrorServerAuthEventArgs<T>, 
            ParamsWSServerAuth, 
            WebsocketHandlerAuth, 
            WSConnectionManagerAuth<T>>, 
        IWebsocketServerAuth<T>
    {
        protected readonly IUserService<T> _userService;

        public WebsocketServerAuth(ParamsWSServerAuth parameters,
            IUserService<T> userService) : base(parameters)
        {
            _userService = userService;

            _handler.AuthorizeEvent += OnAuthorizeEvent;
        }

        public WebsocketServerAuth(ParamsWSServerAuth parameters,
            IUserService<T> userService,
            byte[] certificate,
            string certificatePassword) : base(parameters, certificate, certificatePassword)
        {
            _userService = userService;

            _handler.AuthorizeEvent += OnAuthorizeEvent;
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
                        if (connectionSending == null || connection.ConnectionId != connectionSending.ConnectionId)
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
                        if (connectionSending == null || connection.ConnectionId != connectionSending.ConnectionId)
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

                foreach (var connection in user.Connections.ToList())
                {
                    if (connectionSending == null || connection.ConnectionId != connectionSending.ConnectionId)
                    {
                        await SendToConnectionAsync(message, connection, cancellationToken);
                    }
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

                foreach (var connection in user.Connections.ToList())
                {
                    if (connectionSending == null || connection.ConnectionId != connectionSending.ConnectionId)
                    {
                        await SendToConnectionAsync(message, connection, cancellationToken);
                    }
                }
            }
        }

        public override async Task<bool> SendToConnectionAsync(string message, IConnectionWSServer connection, CancellationToken cancellationToken = default)
        {
            if (_handler.IsServerRunning)
            {
                if (_connectionManager.IsConnectionOpen(connection))
                {
                    return await _handler.SendAsync(message, connection, cancellationToken);
                }
                
                if (_connectionManager.IsConnectionAuthorized(connection))
                {
                    var identity = _connectionManager.GetAllIdentities().FirstOrDefault(s => s.Connections.Any(t => t.ConnectionId == connection.ConnectionId));
                    if (identity != null)
                    {
                        return await _handler.SendAsync(message, connection, cancellationToken);
                    }
                }
            }

            return false;
        }
        public override async Task<bool> SendToConnectionAsync(byte[] message, IConnectionWSServer connection, CancellationToken cancellationToken = default)
        {
            if (_handler != null &&
                _handler.IsServerRunning)
            {
                if (_connectionManager.IsConnectionOpen(connection))
                {
                    return await _handler.SendAsync(message, connection, cancellationToken);
                }

                if (_connectionManager.IsConnectionAuthorized(connection))
                {
                    var identity = _connectionManager.GetAllIdentities().FirstOrDefault(s => s.Connections.Any(t => t.ConnectionId == connection.ConnectionId));
                    if (identity != null)
                    {
                        return await _handler.SendAsync(message, connection, cancellationToken);
                    }
                }
            }

            return false;
        }

        protected override void OnConnectionEvent(object sender, WSConnectionServerEventArgs args)
        {
            switch (args.ConnectionEventType)
            {
                case ConnectionEventType.Connected:
                    if (_connectionManager.IsConnectionAuthorized(args.Connection))
                    {
                        var identity = _connectionManager.GetIdentity(args.Connection);
                        FireEvent(this, new WSConnectionServerAuthEventArgs<T>
                        {
                            ConnectionEventType = args.ConnectionEventType,
                            Connection = args.Connection,
                            UserId = identity.UserId
                        });
                    }
                    else
                    {
                        FireEvent(this, new WSConnectionServerAuthEventArgs<T>
                        {
                            ConnectionEventType = args.ConnectionEventType,
                            Connection = args.Connection
                        });
                    }
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
                    else
                    {
                        FireEvent(this, new WSConnectionServerAuthEventArgs<T>
                        {
                            ConnectionEventType = args.ConnectionEventType,
                            Connection = args.Connection,
                        });
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
        protected override void OnMessageEvent(object sender, WSMessageServerEventArgs args)
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
                    else
                    {
                        FireEvent(this, new WSMessageServerAuthEventArgs<T>
                        {
                            MessageEventType = MessageEventType.Receive,
                            Message = args.Message,
                            Bytes = args.Bytes,
                            UserId = default,
                            Connection = args.Connection,
                        });
                    }
                    break;
                default:
                    break;
            }
        }
        protected override void OnErrorEvent(object sender, WSErrorServerEventArgs args)
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
        protected override void OnTimerPingTick(object state)
        {
            if (!_isPingRunning)
            {
                _isPingRunning = true;

                Task.Run(async () =>
                {
                    foreach (var connection in _connectionManager.GetAllConnections())
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

                    foreach (var identity in _connectionManager.GetAllIdentities())
                    {
                        foreach (var connection in identity.Connections.ToList())
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
                    }

                    _isPingRunning = false;
                }, _cancellationToken);
            }
        }

        protected override WebsocketHandlerAuth CreateWebsocketHandler(byte[] certificate = null, string certificatePassword = null)
        {
            return certificate != null
                ? new WebsocketHandlerAuth(_parameters, certificate, certificatePassword)
                : new WebsocketHandlerAuth(_parameters);
        }
        protected override WSConnectionManagerAuth<T> CreateWSConnectionManager()
        {
            return new WSConnectionManagerAuth<T>();
        }

        public override void Dispose()
        {
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

            if (_handler != null)
            {
                _handler.AuthorizeEvent -= OnAuthorizeEvent;
            }

            base.Dispose();
        }

        public IIdentityWS<T>[] Identities
        {
            get
            {
                return _connectionManager.GetAllIdentities();
            }
        }
    }
}
