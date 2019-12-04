using System;
using System.Net.WebSockets;
using System.Threading.Tasks;
using PHS.Core.Models;
using WebsocketsSimple.Core.Events.Args;
using WebsocketsSimple.Server.Events;

namespace WebsocketsSimple.Server
{
    public interface IWebsocketServer : ICoreNetworking<WSConnectionAuthEventArgs, WSMessageAuthEventArgs, WSErrorEventArgs>
    {
        Task BroadcastToAllUsersAsync(PacketDTO packet);
        Task BroadcastToAllUsersAsync(string message);
        Task BroadcastToAllUsersRawAsync(string message);
        void ConnectClient(Guid userId, WebSocket websocket);
        Task<bool> DisconnectClientAsync(WebSocket websocket);
        Task SendToUserAsync(PacketDTO packet, Guid userId);
        Task SendToUserRawAsync(string message, Guid userId);
        Task SendToWebsocketAsync(PacketDTO packet, WebSocket websocket);
        Task SendToWebsocketRawAsync(string message, WebSocket websocket);

        Task ReceiveAsync(WebSocket websocket, WebSocketReceiveResult result, string message);
    }
}