using Newtonsoft.Json;
using PHS.Core.Enums;
using PHS.Core.Models;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebsocketsSimple.Core.Events.Args;

namespace WebsocketsSimple.Client
{
    public class WebsocketClient :
        CoreNetworking<WSConnectionEventArgs, WSMessageEventArgs, WSErrorEventArgs>,
        IWebsocketClient
    {
        protected ClientWebSocket _client;

        public async Task<bool> Start(string url, int port, string parameters, bool isWSS)
        {
            try
            {
                if (_client != null)
                {
                    if (_client.State == WebSocketState.Open)
                    {
                        await _client.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                    }
                    _client.Dispose();
                }

                _client = new ClientWebSocket();
                var prefix = isWSS ? "wss" : "ws";
                await _client.ConnectAsync(new Uri($"{prefix}://{url}:{port}/{parameters}"), CancellationToken.None);

                if (_client.State == WebSocketState.Open)
                {
                    FireEvent(this, new WSConnectionEventArgs
                    {
                        ArgsType = ArgsType.Connection,
                        ConnectionEventType = ConnectionEventType.ServerStart,
                        Websocket = _client
                    });

                    await Task.Run(async () => await ReceivingAsync(_client));
                    return true;
                }
                else
                {
                    FireEvent(this, new WSConnectionEventArgs
                    {
                        ArgsType = ArgsType.Connection,
                        ConnectionEventType = ConnectionEventType.ServerStop,
                        Websocket = _client
                    });
                }

            }
            catch (Exception ex)
            {
                FireEvent(this, new WSErrorEventArgs
                {
                    ArgsType = ArgsType.Error,
                    Exception = ex,
                    Message = "Error during StartAsync()",
                    Websocket = _client
                });
            }

            return false;
        }
        public virtual async Task<bool> Send(string message)
        {
            try
            {
                if (_client != null &&
                    _client.State == WebSocketState.Open)
                {
                    var bytes = Encoding.UTF8.GetBytes(message);

                    await _client.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);

                    FireEvent(this, new WSMessageEventArgs
                    {
                        ArgsType = ArgsType.Message,
                        MessageEventType = MessageEventType.Sent,
                        Message = message,
                        Packet = new PacketDTO
                        {
                            Action = (int)ActionType.SendToServer,
                            Data = message,
                            Timestamp = DateTime.UtcNow
                        },
                        Websocket = _client
                    });

                    return true;
                }
            }
            catch (Exception ex)
            {
                FireEvent(this, new WSErrorEventArgs
                {
                    ArgsType = ArgsType.Error,
                    Exception = ex,
                    Message = "Error during SendAsync()",
                    Websocket = _client
                });
            }

            return false;
        }
        public virtual async Task<bool> Send(PacketDTO packet)
        {
            try
            {
                if (_client != null &&
                    _client.State == WebSocketState.Open)
                {
                    var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(packet));

                    await _client.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);

                    FireEvent(this, new WSMessageEventArgs
                    {
                        ArgsType = ArgsType.Message,
                        MessageEventType = MessageEventType.Sent,
                        Message = JsonConvert.SerializeObject(packet),
                        Packet = packet,
                        Websocket = _client
                    });

                    return true;
                }
            }
            catch (Exception ex)
            {
                FireEvent(this, new WSErrorEventArgs
                {
                    ArgsType = ArgsType.Error,
                    Exception = ex,
                    Message = "Error during SendAsync()",
                    Websocket = _client
                });
            }

            return false;
        }
        public virtual async Task<bool> StopAsync()
        {
            try
            {
                if (_client != null &&
                    _client.State == WebSocketState.Open)
                {
                    await _client.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);

                    FireEvent(this, new WSConnectionEventArgs
                    {
                        ArgsType = ArgsType.Connection,
                        ConnectionEventType = ConnectionEventType.ServerStop,
                        Websocket = _client
                    });

                    return true;
                }
            }
            catch (Exception ex)
            {
                FireEvent(this, new WSErrorEventArgs
                {
                    ArgsType = ArgsType.Error,
                    Exception = ex,
                    Message = "Error in StopAsync()",
                    Websocket = _client
                });
            }

            return false;
        }

        private async Task ReceivingAsync(ClientWebSocket client)
        {
            try
            {
                var buffer = new byte[1024 * 4];

                while (true)
                {
                    var result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        PacketDTO packet;

                        try
                        {
                            packet = JsonConvert.DeserializeObject<PacketDTO>(message);
                        }
                        catch
                        {
                            packet = new PacketDTO
                            {
                                Action = (int)ActionType.SendToClient,
                                Data = message,
                                Timestamp = DateTime.UtcNow
                            };
                        }

                        FireEvent(this, new WSMessageEventArgs
                        {
                            ArgsType = ArgsType.Message,
                            Message = message,
                            Packet = packet,
                            Websocket = _client,
                            MessageEventType = MessageEventType.Receive
                        });
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await StopAsync();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                FireEvent(this, new WSErrorEventArgs
                {
                    ArgsType = ArgsType.Error,
                    Exception = ex,
                    Message = "Error in ReceiveAsync()",
                    Websocket = _client
                });
            }
        }
        public bool IsRunning
        {
            get
            {
                return _client != null &&
                    _client.State == WebSocketState.Open;
            }
        }
    }
}
