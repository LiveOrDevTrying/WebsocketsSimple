using Newtonsoft.Json;
using PHS.Networking.Enums;
using PHS.Networking.Models;
using PHS.Networking.Services;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebsocketsSimple.Client.Events.Args;
using WebsocketsSimple.Client.Models;
using WebsocketsSimple.Core.Models;

namespace WebsocketsSimple.Client
{
    public class WebsocketClient :
        CoreNetworking<WSConnectionClientEventArgs, WSMessageClientEventArgs, WSErrorClientEventArgs>,
        IWebsocketClient
    {
        protected IConnection _connection;
        protected IParamsWSClient _parameters;
        protected string _oauthToken;

        public WebsocketClient(IParamsWSClient parameters, string oauthToken = "")
        {
            _parameters = parameters;
            _oauthToken = oauthToken;
        }

        public async Task<bool> ConnectAsync()
        {
            try
            {
                if (_connection != null &&
                    _connection.Websocket != null)
                {
                    if (_connection.Websocket.State == WebSocketState.Open)
                    {
                        await _connection.Websocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                    }
                    _connection.Websocket.Dispose();
                    _connection = null;
                }

                var client = new ClientWebSocket();
                var prefix = _parameters.IsWebsocketSecured ? "wss" : "ws";

                if (string.IsNullOrWhiteSpace(_oauthToken))
                {
                    await client.ConnectAsync(new Uri($"{prefix}://{_parameters.Uri}:{_parameters.Port}"), CancellationToken.None);

                }
                else
                {
                    await client.ConnectAsync(new Uri($"{prefix}://{_parameters.Uri}:{_parameters.Port}/{_oauthToken}"), CancellationToken.None);
                }

                if (client.State == WebSocketState.Open)
                {
                    _connection = new Connection
                    {
                        Websocket = client
                    };

                    FireEvent(this, new WSConnectionClientEventArgs
                    {
                        ConnectionEventType = ConnectionEventType.Connected,
                        Connection = _connection
                    });

                    ReceiveAsync();
                    return true;
                }
                else
                {
                    FireEvent(this, new WSConnectionClientEventArgs
                    {
                        ConnectionEventType = ConnectionEventType.Disconnect,
                        Connection = _connection,
                    });
                }

            }
            catch (Exception ex)
            {
                FireEvent(this, new WSErrorClientEventArgs
                {
                    Exception = ex,
                    Message = "Error during StartAsync()",
                    Connection = _connection
                });
            }

            return false;
        }
        public async Task<bool> DisconnectAsync()
        {
            try
            {
                if (_connection != null &&
                    _connection.Websocket != null &&
                    _connection.Websocket.State == WebSocketState.Open)
                {
                    await _connection.Websocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);

                    FireEvent(this, new WSConnectionClientEventArgs
                    {
                        ConnectionEventType = ConnectionEventType.Disconnect,
                        Connection = _connection
                    });

                    return true;
                }
            }
            catch (Exception ex)
            {
                FireEvent(this, new WSErrorClientEventArgs
                {
                    Exception = ex,
                    Message = "Error in StopAsync()",
                    Connection = _connection
                });
            }

            return false;
        }
        
        public async Task<bool> SentToServerAsync<T>(T packet) where T : IPacket
        {
            try
            {
                if (_connection != null &&
                    _connection.Websocket != null &&
                    _connection.Websocket.State == WebSocketState.Open)
                {
                    var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(packet));

                    await _connection.Websocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);

                    FireEvent(this, new WSMessageClientEventArgs
                    {
                        MessageEventType = MessageEventType.Sent,
                        Message = JsonConvert.SerializeObject(packet),
                        Packet = packet,
                        Connection = _connection,
                    });

                    return true;
                }
            }
            catch (Exception ex)
            {
                FireEvent(this, new WSErrorClientEventArgs
                {
                    Exception = ex,
                    Message = "Error during SendAsync()",
                    Connection = _connection
                });
            }

            return false;
        }
        public async Task<bool> SendToServerAsync(string message)
        {
            return await SentToServerAsync(new Packet
            {
                Data = message,
                Timestamp = DateTime.UtcNow
            });
        }
        public async Task<bool> SendToServerRawAsync(string message)
        {
            try
            {
                if (_connection != null &&
                    _connection.Websocket != null &&
                    _connection.Websocket.State == WebSocketState.Open)
                {
                    var bytes = Encoding.UTF8.GetBytes(message);

                    await _connection.Websocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);

                    FireEvent(this, new WSMessageClientEventArgs
                    {
                        MessageEventType = MessageEventType.Sent,
                        Message = message,
                        Packet = new Packet
                        {
                            Data = message,
                            Timestamp = DateTime.UtcNow
                        },
                        Connection = _connection
                    });

                    return true;
                }
            }
            catch (Exception ex)
            {
                FireEvent(this, new WSErrorClientEventArgs
                {
                    Exception = ex,
                    Message = "Error during SendAsync()",
                    Connection = _connection
                });
            }

            return false;
        }

        private async Task ReceiveAsync()
        {
            try
            {
                var buffer = new byte[1024 * 4];

                var isRunning = true;

                while (isRunning)
                {
                    var result = await _connection.Websocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);

                        if (!string.IsNullOrWhiteSpace(message))
                        {
                            if (message.Trim().ToLower() == "ping")
                            {
                                await SendToServerRawAsync("pong");
                            }
                            else 
                            {
                                var packet = MessageReceived(message);

                                FireEvent(this, new WSMessageClientEventArgs
                                {
                                    Message = message,
                                    Packet = packet,
                                    Connection = _connection,
                                    MessageEventType = MessageEventType.Receive
                                });
                            }
                        }
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await DisconnectAsync();
                        isRunning = false;
                    }
                }
            }
            catch (Exception ex)
            {
                FireEvent(this, new WSErrorClientEventArgs
                {
                    Exception = ex,
                    Message = "Error in ReceiveAsync()",
                    Connection = _connection
                });
            }
        }

        protected virtual IPacket MessageReceived(string message)
        {
            IPacket packet;

            try
            {
                packet = JsonConvert.DeserializeObject<Packet>(message);

                if (string.IsNullOrWhiteSpace(packet.Data))
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

        public bool IsRunning
        {
            get
            {
                return _connection != null &&
                    _connection.Websocket != null &&
                    _connection.Websocket.State == WebSocketState.Open;
            }
        }
        public IConnection Connection
        {
            get
            {
                return _connection;
            }
        }
    }
}
