using PHS.Core.Enums;
using PHS.Core.Models;
using PHS.Tcp.Core.Async.Server.Models;
using WebsocketsSimple.Server.Events;
using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using WebsocketsSimple.Core.Events.Args;
using WebsocketsSimple.Core.Models;

namespace WebsocketsSimple.Server
{
    public class WebsocketServer :
        CoreNetworking<WSConnectionAuthEventArgs, WSMessageAuthEventArgs, WSErrorEventArgs>,
        IWebsocketServer
    {
        protected readonly ParamsWSServer _parameters;
        protected readonly WebsocketHandler _handler;
        protected readonly WebsocketConnectionManager _connectionManager;

        protected Timer _timerPing;
        protected bool _isPingRunning;

        public WebsocketServer(ParamsWSServer parameters,
            WebsocketConnectionManager connectionManager)
        {
            _parameters = parameters;
            _connectionManager = connectionManager;

            _handler = new WebsocketHandler();
            _handler.ConnectionEvent += OnConnectionEvent;
            _handler.MessageEvent += OnMessageEvent;
            _handler.ErrorEvent += OnErrorEvent;
        }

        public virtual async Task BroadcastToAllUsersAsync(PacketDTO packet)
        {
            foreach (var identity in _connectionManager.GetAll())
            {
                foreach (var connection in identity.Connections)
                {
                    if (connection.Websocket.State == WebSocketState.Open)
                    {
                        await SendToWebsocketAsync(packet, connection.Websocket);
                    }
                }
            }
        }
        public virtual async Task BroadcastToAllUsersAsync(string message)
        {
            var packet = new PacketDTO
            {
                Action = (int)ActionType.BroadcastAllConnectedClients,
                Data = message,
                Timestamp = DateTime.UtcNow
            };

            await BroadcastToAllUsersAsync(packet);
        }
        public virtual async Task BroadcastToAllUsersRawAsync(string message)
        {
            foreach (var identity in _connectionManager.GetAll())
            {
                foreach (var connection in identity.Connections)
                {
                    if (connection.Websocket.State == WebSocketState.Open)
                    {
                        await SendToWebsocketRawAsync(message, connection.Websocket);
                    }
                }
            }
        }

        public virtual void ConnectClient(Guid userId, WebSocket websocket)
        {
            _handler.ConnectClient(userId, websocket);
        }
        public virtual async Task<bool> DisconnectClientAsync(WebSocket websocket)
        {
            if (_connectionManager.IsWebsocketInClients(websocket))
            {
                var identity = _connectionManager.GetIdentity(websocket);
                var connection = identity.GetConnection(websocket);
                await _connectionManager.RemoveWebsocketAsync(websocket);

                FireEvent(this, new WSConnectionAuthEventArgs
                {
                    ConnectionEventType = ConnectionEventType.Disconnect,
                    ArgsType = ArgsType.Connection,
                    UserId = identity.UserId,
                    Websocket = websocket,
                    BLLConnectionEventType = Enums.BLLConnectionEventType.Disconnect,
                    Connection = connection
                });

                return true;
            }

            return false;
        }

        public async Task SendToUserAsync(PacketDTO packet, Guid userId)
        {
            var identity = _connectionManager.GetSockets(userId);

            foreach (var connection in identity.Connections)
            {
                await _handler.SendAsync(packet, connection.Websocket);
            }
        }
        public async Task SendToUserRawAsync(string message, Guid userId)
        {
            var identity = _connectionManager.GetSockets(userId);

            foreach (var connection in identity.Connections)
            {
                await _handler.SendRawAsync(message, connection.Websocket);
            }
        }

        public async Task SendToWebsocketAsync(PacketDTO packet, WebSocket websocket)
        {
            await _handler.SendAsync(packet, websocket);
        }
        public async Task SendToWebsocketRawAsync(string message, WebSocket websocket)
        {
            await _handler.SendRawAsync(message, websocket);
        }

        public virtual async Task ReceiveAsync(WebSocket websocket, WebSocketReceiveResult result, string message)
        {
            await _handler.ReceiveAsync(websocket, result, message);
        }

        protected virtual async Task OnConnectionEvent(object sender, WSConnectionEventArgs args)
        {
            switch (args.ConnectionEventType)
            {
                case ConnectionEventType.Connected:
                    switch (args)
                    {
                        case WSConnectionAuthEventArgs c:
                            var identity = _connectionManager.AddWebSocket(c.UserId, args.Websocket);

                            FireEvent(this, new WSConnectionAuthEventArgs
                            {
                                Websocket = args.Websocket,
                                BLLConnectionEventType = Enums.BLLConnectionEventType.ConnectedAuthorized,
                                Connection = identity.GetConnection(args.Websocket),
                                UserId = identity.UserId,
                                ArgsType = ArgsType.Connection,
                                ConnectionEventType = ConnectionEventType.Connected
                            });
                            break;
                        default:
                            break;
                    }
                    
                    break;
                case ConnectionEventType.Disconnect:
                    var idy = _connectionManager.GetIdentity(args.Websocket);
                    var connection = idy.GetConnection(args.Websocket);
                    await _connectionManager.RemoveWebsocketAsync(args.Websocket);

                    FireEvent(this, new WSConnectionAuthEventArgs
                    {
                        Websocket = args.Websocket,
                        BLLConnectionEventType = Enums.BLLConnectionEventType.Disconnect,
                        Connection = connection,
                        UserId = idy.UserId,
                        ConnectionEventType = ConnectionEventType.Disconnect,
                        ArgsType = ArgsType.Connection,
                    });
                    break;
                case ConnectionEventType.ServerStart:
                    if (_timerPing != null)
                    {
                        _timerPing.Dispose();
                        _timerPing = null;
                    }

                    _timerPing = new Timer(OnTimerPingTick, null, _parameters.PingIntervalSec * 1000, _parameters.PingIntervalSec * 1000);

                    FireEvent(this, new WSConnectionAuthEventArgs
                    {
                        ConnectionEventType = args.ConnectionEventType,
                        ArgsType = ArgsType.Connection,
                        BLLConnectionEventType = Enums.BLLConnectionEventType.ServerStart,
                    });
                    break;
                case ConnectionEventType.ServerStop:
                    if (_timerPing != null)
                    {
                        _timerPing.Dispose();
                        _timerPing = null;
                    }

                    FireEvent(this, new WSConnectionAuthEventArgs
                    {
                        BLLConnectionEventType = Enums.BLLConnectionEventType.ServerStop,
                        ConnectionEventType = args.ConnectionEventType,
                        ArgsType = ArgsType.Connection,
                    });
                    break;
                case ConnectionEventType.Connecting:
                    FireEvent(this, new WSConnectionAuthEventArgs
                    {
                        ConnectionEventType = args.ConnectionEventType,
                        ArgsType = ArgsType.Connection,
                        BLLConnectionEventType = Enums.BLLConnectionEventType.Connecting
                    });
                    break;
                case ConnectionEventType.MaxConnectionsReached:
                    FireEvent(this, new WSConnectionAuthEventArgs
                    {
                        ConnectionEventType = args.ConnectionEventType,
                        ArgsType = ArgsType.Connection,
                        BLLConnectionEventType = Enums.BLLConnectionEventType.MaxConnectionsReached
                    });
                    break;
                default:
                    break;
            }
        }
        protected virtual Task OnErrorEvent(object sender, WSErrorEventArgs args)
        {
            FireEvent(this, args);
            return Task.CompletedTask;
        }
        protected virtual Task OnMessageEvent(object sender, WSMessageEventArgs args)
        {
            var identity = _connectionManager.GetIdentity(args.Websocket);
            var connection = identity.GetConnection(args.Websocket);

            FireEvent(this, new WSMessageAuthEventArgs
            {
                ArgsType = args.ArgsType,
                Timestamp = args.Timestamp,
                Message = args.Message,
                MessageEventType = args.MessageEventType,
                Connection = connection,
                Packet = args.Packet,
                Websocket = args.Websocket,
                UserId = identity.UserId,
            });

            return Task.CompletedTask;
        }

        protected virtual void OnTimerPingTick(object state)
        {
            if (!_isPingRunning)
            {
                _isPingRunning = true;

                Task.Run(async () =>
                {
                    await PingClientsAsync();
                    _isPingRunning = false;
                });
            }
        }
        protected virtual async Task PingClientsAsync()
        {
            foreach (var identity in _connectionManager.GetAll())
            {
                var connectionsToRemove = new List<ConnectionWebsocketDTO>();

                foreach (var connection in identity.Connections)
                {
                    if (connection.HasBeenPinged)
                    {
                        // Already been pinged, no response, disconnect
                        connectionsToRemove.Add(connection);
                    }
                    else
                    {
                        connection.HasBeenPinged = true;
                        await _handler.SendRawAsync("Ping", connection.Websocket);
                    }
                }

                foreach (var connectionToRemove in connectionsToRemove)
                {
                    await _handler.SendRawAsync("No ping response - disconnected.", connectionToRemove.Websocket);
                    await DisconnectClientAsync(connectionToRemove.Websocket);
                }
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
    }
}