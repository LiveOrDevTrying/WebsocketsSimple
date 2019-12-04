using WebsocketsSimple.Server.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using WebsocketsSimple.Core.Models;

namespace WebsocketsSimple.Server
{
    public class WebsocketConnectionManager
    {
        protected ConcurrentDictionary<Guid, IUserConnectionWSDTO> _clients = 
            new ConcurrentDictionary<Guid, IUserConnectionWSDTO>();

        public IUserConnectionWSDTO GetSockets(Guid userId)
        {
            return _clients.Values
                .FirstOrDefault(p => p.UserId == userId);
        }
        public IUserConnectionWSDTO GetIdentity(WebSocket websocket)
        {
            if (_clients.Any(p => p.Value.Connections.Any(t => t.Websocket.GetHashCode() == websocket.GetHashCode())))
            {
                var client = _clients.Values.FirstOrDefault(s => s.Connections.Any(t => t.Websocket.GetHashCode() == websocket.GetHashCode()));
                return client;
            }

            return default;
        }
        public ConnectionWebsocketDTO GetConnection(WebSocket websocket)
        {
            if (_clients.Any(p => p.Value.Connections.Any(t => t.Websocket.GetHashCode() == websocket.GetHashCode())))
            {
                var client = _clients.Values.FirstOrDefault(s => s.Connections.Any(t => t.Websocket.GetHashCode() == websocket.GetHashCode()));
                return client.Connections.First(s => s.GetHashCode() == websocket.GetHashCode());
            }

            return default;
        }
        public ICollection<IUserConnectionWSDTO> GetAll()
        {
            return _clients.Values.ToList();
        }

        public IUserConnectionWSDTO AddWebSocket(Guid applicationUserId, WebSocket websocket)
        {
            IUserConnectionWSDTO client;

            if (_clients.ContainsKey(applicationUserId))
            {
                client = _clients.First(s => s.Key == applicationUserId).Value;
            }
            else
            {
                client = new UserConnectionWSDTO
                {
                    UserId = applicationUserId,
                    Connections = new List<ConnectionWebsocketDTO>()
                };
                _clients.TryAdd(applicationUserId, client);
            }

            client.Connections.Add(new ConnectionWebsocketDTO
            {
                Websocket = websocket,
            });
            return client;
        }
        public async Task RemoveWebsocketAsync(ConnectionWebsocketDTO connection)
        {
            if (_clients.Any(p => p.Value.Connections.Any(t => t.GetHashCode() == connection.GetHashCode())))
            {
                var client = _clients.First(s => s.Value.Connections.Any(t => t.GetHashCode() == connection.GetHashCode())).Value;
                client.Connections.Remove(connection);

                if (!client.Connections.Any())
                {
                    _clients.TryRemove(client.UserId, out client);
                }
            }

            if (connection.Websocket.State == WebSocketState.Open)
            {
                await connection.Websocket.CloseAsync(closeStatus: WebSocketCloseStatus.NormalClosure,
                                        statusDescription: "Closed by the WebSocketManager",
                                        cancellationToken: CancellationToken.None);
            }
        }
        public async Task RemoveWebsocketAsync(WebSocket websocket)
        {
            if (_clients.Any(p => p.Value.Connections.Any(t => t.Websocket.GetHashCode() == websocket.GetHashCode())))
            {
                var client = _clients.First(s => s.Value.Connections.Any(t => t.Websocket.GetHashCode() == websocket.GetHashCode())).Value;
                var connection = client.Connections.First(s => s.Websocket.GetHashCode() == websocket.GetHashCode());
                await RemoveWebsocketAsync(connection);
            }

        }
        public bool IsWebsocketInClients(WebSocket websocket)
        {
            return _clients.Values.Any(s => s.Connections.Any(t => t.Websocket.GetHashCode() == websocket.GetHashCode()));
        }
    }
}
