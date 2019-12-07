using Newtonsoft.Json;
using PHS.Core.Enums;
using PHS.Core.Models;
using WebsocketsSimple.Server.Events;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebsocketsSimple.Core.Events.Args;

namespace WebsocketsSimple.Server
{
    public sealed class WebsocketHandler :
        CoreNetworking<WSConnectionEventArgs, WSMessageEventArgs, WSErrorEventArgs>,
        ICoreNetworking<WSConnectionEventArgs, WSMessageEventArgs, WSErrorEventArgs>
    {
        public void ConnectClient(Guid userId, WebSocket websocket)
        {
            FireEvent(this, new WSConnectionAuthEventArgs
            {
                ConnectionEventType = ConnectionEventType.Connected,
                ArgsType = ArgsType.Connection,
                UserId = userId,
                Websocket = websocket,
            });
        }
        public Task DisconnectClient(WebSocket websocket)
        {
            FireEvent(this, new WSConnectionEventArgs
            {
                ConnectionEventType = ConnectionEventType.Disconnect,
                ArgsType = ArgsType.Connection,
                Websocket = websocket,
            });

            return Task.CompletedTask;
        }

        public async Task SendAsync(PacketDTO packet, WebSocket websocket)
        {
            if (websocket.State != WebSocketState.Open)
            {
                return;
            }

            var message = JsonConvert.SerializeObject(packet);

            await websocket.SendAsync(buffer: new ArraySegment<byte>(array: Encoding.UTF8.GetBytes(message),
                                                                  offset: 0,
                                                                  count: message.Length),
                                                                  messageType: WebSocketMessageType.Text,
                                                                  endOfMessage: true,
                                                                  cancellationToken: CancellationToken.None);

            FireEvent(this, new WSMessageEventArgs
            {
                ArgsType = ArgsType.Message,
                MessageEventType = MessageEventType.Sent,
                Message = message,
                Packet = packet,
                Websocket = websocket,
            });
        }
        public async Task SendAsync(string message, WebSocket websocket)
        {
            var packet = new PacketDTO
            {
                Action = (int)ActionType.SendToClient,
                Data = message,
                Timestamp = DateTime.UtcNow
            };

            await SendAsync(packet, websocket);
        }
        public async Task SendRawAsync(string message, WebSocket websocket)
        {
            if (websocket.State != WebSocketState.Open)
            {
                return;
            }

            await websocket.SendAsync(buffer: new ArraySegment<byte>(array: Encoding.UTF8.GetBytes(message),
                                                                  offset: 0,
                                                                  count: message.Length),
                                                                  messageType: WebSocketMessageType.Text,
                                                                  endOfMessage: true,
                                                                  cancellationToken: CancellationToken.None);

            FireEvent(this, new WSMessageEventArgs
            {
                ArgsType = ArgsType.Message,
                MessageEventType = MessageEventType.Sent,
                Message = message,
                Packet = new PacketDTO
                {
                    Action = (int)ActionType.SendToClient,
                    Data = message,
                    Timestamp = DateTime.UtcNow
                },
                Websocket = websocket,
            });
        }
        
        public Task ReceiveAsync(WebSocket websocket, WebSocketReceiveResult result, string message)
        {
            PacketDTO packet;

            try
            {
                packet = JsonConvert.DeserializeObject<PacketDTO>(message);

            }
            catch
            {
                packet = new PacketDTO
                {
                    Action = (int)ActionType.SendToServer,
                    Data = message,
                    Timestamp = DateTime.UtcNow
                };
            }

            FireEvent(this, new WSMessageEventArgs
            {
                ArgsType = ArgsType.Message,
                Timestamp = DateTime.UtcNow,
                Message = message,
                MessageEventType = MessageEventType.Receive,
                Websocket = websocket,
                Packet = packet
            });

            return Task.CompletedTask;
        }
    }
}