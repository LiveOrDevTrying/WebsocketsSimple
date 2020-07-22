using Newtonsoft.Json;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PHS.Networking.Services;
using WebsocketsSimple.Server.Events.Args;
using WebsocketsSimple.Server.Models;
using PHS.Networking.Enums;
using PHS.Networking.Models;
using PHS.Tcp.Core.Async.Server.Models;
using PHS.Networking.Events;
using PHS.Networking.Server.Events.Args;
using PHS.Networking.Server.Enums;

namespace WebsocketsSimple.Server.Handlers
{
    public class WebsocketHandler :
        CoreNetworking<WSConnectionServerEventArgs, WSMessageServerEventArgs, WSErrorServerEventArgs>,
        ICoreNetworking<WSConnectionServerEventArgs, WSMessageServerEventArgs, WSErrorServerEventArgs>
    {
        protected readonly IParamsWSServer _parameters;
        protected int _numberOfConnections;

        private event NetworkingEventHandler<ServerEventArgs> _serverEvent;

        public WebsocketHandler(IParamsWSServer parameters)
        {
            _parameters = parameters;
        }

        public virtual async Task SendAsync<T>(T packet, IConnectionServer connection) where T : IPacket
        {
            try
            {
                var message = JsonConvert.SerializeObject(packet);

                await connection.Websocket.SendAsync(buffer: new ArraySegment<byte>(array: Encoding.UTF8.GetBytes(message),
                    offset: 0,
                    count: message.Length),
                    messageType: WebSocketMessageType.Text,
                    endOfMessage: true,
                    cancellationToken: CancellationToken.None);

                await FireEventAsync(this, new WSMessageServerEventArgs
                {
                    MessageEventType = MessageEventType.Sent,
                    Message = message,
                    Packet = packet,
                    Connection = connection,
                });
            }
            catch (Exception ex)
            {
                await FireEventAsync(this, new WSErrorServerEventArgs
                {
                    Exception = ex,
                    Message = ex.Message,
                    Connection = connection,
                });

                await DisconnectConnectionAsync(connection);
            }
        }
        public virtual async Task SendAsync(string message, IConnectionServer connection)
        {
            var packet = new Packet
            {
                Data = message,
                Timestamp = DateTime.UtcNow
            };

            await SendAsync(packet, connection);
        }
        public virtual async Task SendRawAsync(string message, IConnectionServer connection)
        {
            try
            {
                await connection.Websocket.SendAsync(buffer: new ArraySegment<byte>(array: Encoding.UTF8.GetBytes(message),
                    offset: 0,
                    count: message.Length),
                    messageType: WebSocketMessageType.Text,
                    endOfMessage: true,
                    cancellationToken: CancellationToken.None);

                await FireEventAsync(this, new WSMessageServerEventArgs
                {
                    MessageEventType = MessageEventType.Sent,
                    Message = message,
                    Packet = new Packet
                    {
                        Data = message,
                        Timestamp = DateTime.UtcNow
                    },
                    Connection = connection,
                });
            }
            catch (Exception ex)
            {
                _numberOfConnections--;
                await FireEventAsync(this, new WSErrorServerEventArgs
                {
                    Connection = connection,
                    Exception = ex,
                    Message = ex.Message,
                });
            }
        }

        public virtual async Task StartReceivingAsync(IConnectionServer connection)
        {
            try
            {
                _numberOfConnections++;

                await Receive(connection, async (result, message) =>
                {
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        if (!string.IsNullOrWhiteSpace(message))
                        {
                            if (message.Trim().ToLower() == "pong")
                            {
                                connection.HasBeenPinged = false;
                            }
                            else
                            {
                                var packet = MessageReceived(message, connection);

                                await FireEventAsync(this, new WSMessageServerEventArgs
                                {
                                    Message = message,
                                    MessageEventType = MessageEventType.Receive,
                                    Packet = packet,
                                    Connection = connection,
                                });
                            }
                        }
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await FireEventAsync(this, new WSConnectionServerEventArgs
                        {
                            Connection = connection,
                            ConnectionEventType = ConnectionEventType.Disconnect,
                        });
                        return;
                    }
                });
            }
            catch (Exception ex)
            {
                _numberOfConnections--;
                await FireEventAsync(this, new WSErrorServerEventArgs
                {
                    Connection = connection,
                    Exception = ex,
                    Message = ex.Message,
                });
            }
        }
        protected virtual async Task Receive(IConnectionServer connection, Action<WebSocketReceiveResult, string> handleMessage)
        {
            var buffer = new byte[1024 * 4];

            while (connection.Websocket.State == WebSocketState.Open)
            {
                try
                {
                    var result = await connection.Websocket.ReceiveAsync(buffer: new ArraySegment<byte>(buffer),
                        cancellationToken: CancellationToken.None);

                    handleMessage(result, Encoding.UTF8.GetString(buffer, 0, result.Count));
                }
                catch (Exception ex)
                {
                    _numberOfConnections--;
                    await FireEventAsync(this, new WSErrorServerEventArgs
                    {
                        Connection = connection,
                        Exception = ex,
                        Message = ex.Message,
                    });
                }
            }
        }
        protected virtual IPacket MessageReceived(string message, IConnectionServer connection)
        {
            IPacket packet;

            try
            {
                packet = JsonConvert.DeserializeObject<Packet>(message);

                if (string.IsNullOrEmpty(packet.Data))
                {
                    packet = new Packet
                    {
                        Data = message,
                        Timestamp = DateTime.UtcNow
                    };
                }
            }
            catch
            {
                packet = new Packet
                {
                    Data = message,
                    Timestamp = DateTime.UtcNow
                };
            }

            return packet;
        }

        public virtual async Task<bool> DisconnectConnectionAsync(IConnectionServer connection)
        {
            try
            {
                _numberOfConnections--;

                await FireEventAsync(this, new WSConnectionServerEventArgs
                {
                    Connection = connection,
                    ConnectionEventType = ConnectionEventType.Disconnect
                });
                return true;
            }
            catch (Exception ex)
            {
                await FireEventAsync(this, new WSErrorServerEventArgs
                {
                    Connection = connection,
                    Exception = ex,
                    Message = ex.Message
                });
            }
            return false;
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
            FireEventAsync(this, new ServerEventArgs
            {
                ServerEventType = ServerEventType.Stop
            }).Wait();
        }

        public int NumberOfConnections
        {
            get
            {
                return _numberOfConnections;
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