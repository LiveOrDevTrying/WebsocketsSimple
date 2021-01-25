using WebsocketsSimple.Server.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace WebsocketsSimple.Server.Managers
{
    public class WebsocketConnectionManagerAuth<T> : WebsocketConnectionManager
    {
        protected ConcurrentDictionary<T, IUserConnectionsWS<T>> _identites =
            new ConcurrentDictionary<T, IUserConnectionsWS<T>>();

        public IUserConnectionsWS<T> GetIdentity(T userId)
        {
            return _identites.TryGetValue(userId, out var identity) ? identity : null;
        }
        public IUserConnectionsWS<T> GetIdentity(IConnectionWSServer connection)
        {
            return _identites.Any(p => p.Value.Connections.Any(t => t != null && t.Websocket != null && t.Websocket.GetHashCode() == connection.Websocket.GetHashCode()))
               ? _identites.Values.FirstOrDefault(s => s.Connections.Any(t => t.Websocket.GetHashCode() == connection.Websocket.GetHashCode()))
               : (default);
        }
        public IUserConnectionsWS<T>[] GetAllIdentities()
        {
            return _identites.Values.ToArray();
        }

        public IUserConnectionsWS<T> AddIdentity(T userId, IConnectionWSServer connection)
        {
            if (!_identites.TryGetValue(userId, out var userConnection))
            {
                userConnection = new UserConnectionsWS<T>
                {
                    Connections = new List<IConnectionWSServer>(),
                    UserId = userId
                };
                _identites.TryAdd(userId, userConnection);
            }

            if (!userConnection.Connections.Any(s => s != null && s.Websocket != null && s.Websocket.GetHashCode() == connection.Websocket.GetHashCode()))
            {
                userConnection.Connections.Add(connection);
                return userConnection;
            }

            return null;
        }
        public async Task RemoveIdentityAsync(IConnectionWSServer connection, bool disconnectConnection)
        {
            var userConnection = _identites.Values.FirstOrDefault(s => s.Connections.Any(t => t.GetHashCode() == connection.GetHashCode()));

            if (userConnection != null)
            {
                var instance = userConnection.Connections.FirstOrDefault(s => s != null && s.Websocket != null && s.Websocket.GetHashCode() == connection.Websocket.GetHashCode());

                if (instance != null)
                {
                    userConnection.Connections.Remove(connection);

                    if (!userConnection.Connections.Any())
                    {
                        _identites.TryRemove(userConnection.UserId, out userConnection);
                    }
                }
            }

            if (disconnectConnection &&
                connection.Websocket != null &&
                connection.Websocket.State == WebSocketState.Open)
            {
                await connection.Websocket.CloseAsync(closeStatus: WebSocketCloseStatus.NormalClosure,
                    statusDescription: "Websocket connection closed",
                    cancellationToken: CancellationToken.None);
            }
        }

        public bool IsConnectionAuthorized(IConnectionWSServer connection)
        {
            var userConnection = _identites.Values.FirstOrDefault(s => s.Connections.Any(t => t.Websocket.GetHashCode() == connection.Websocket.GetHashCode()));

            if (userConnection != null)
            {
                var instance = userConnection.Connections.FirstOrDefault(s => s.Websocket.GetHashCode() == connection.Websocket.GetHashCode());

                if (instance != null &&
                    instance.Websocket != null)
                {
                    return instance.Websocket.State == WebSocketState.Open;
                }
            }

            return false;
        }
        public bool IsUserConnected(T userId)
        {
            return _identites.ContainsKey(userId);
        }
    }
}
