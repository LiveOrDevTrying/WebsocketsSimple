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
        protected ConcurrentDictionary<T, IUserConnectionsWS<T>> _userConnections =
            new ConcurrentDictionary<T, IUserConnectionsWS<T>>();

        public IUserConnectionsWS<T> GetIdentity(T userId)
        {
            return _userConnections.TryGetValue(userId, out var identity) ? identity : null;
        }
        public IUserConnectionsWS<T> GetIdentity(IConnectionWSServer connection)
        {
            return _userConnections.Any(p => p.Value.Connections.Any(t => t != null && t.Websocket != null && t.Websocket.GetHashCode() == connection.Websocket.GetHashCode()))
               ? _userConnections.Values.FirstOrDefault(s => s.Connections.Any(t => t.Websocket.GetHashCode() == connection.Websocket.GetHashCode()))
               : (default);
        }
        public IUserConnectionsWS<T>[] GetAllIdentities()
        {
            return _userConnections.Values.ToArray();
        }

        public IUserConnectionsWS<T> AddUserConnection(T userId, IConnectionWSServer connection)
        {
            if (!_userConnections.TryGetValue(userId, out var userConnection))
            {
                userConnection = new UserConnectionsWS<T>
                {
                    Connections = new List<IConnectionWSServer>(),
                    UserId = userId
                };
                _userConnections.TryAdd(userId, userConnection);
            }

            if (!userConnection.Connections.Any(s => s != null && s.Websocket != null && s.Websocket.GetHashCode() == connection.Websocket.GetHashCode()))
            {
                userConnection.Connections.Add(connection);
                return userConnection;
            }

            return null;
        }
        public async Task RemoveUserConnectionAsync(IConnectionWSServer connection, bool disconnectConnection)
        {
            var userConnection = _userConnections.Values.FirstOrDefault(s => s.Connections.Any(t => t.GetHashCode() == connection.GetHashCode()));

            if (userConnection != null)
            {
                var instance = userConnection.Connections.FirstOrDefault(s => s != null && s.Websocket != null && s.Websocket.GetHashCode() == connection.Websocket.GetHashCode());

                if (instance != null)
                {
                    userConnection.Connections.Remove(connection);

                    if (!userConnection.Connections.Any())
                    {
                        _userConnections.TryRemove(userConnection.UserId, out userConnection);
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
            var userConnection = _userConnections.Values.FirstOrDefault(s => s.Connections.Any(t => t.Websocket.GetHashCode() == connection.Websocket.GetHashCode()));

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
            return _userConnections.ContainsKey(userId);
        }
    }
}
