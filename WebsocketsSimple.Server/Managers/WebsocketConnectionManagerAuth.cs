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
    internal class WebsocketConnectionManagerAuth<T> : WebsocketConnectionManager
    {
        protected ConcurrentDictionary<T, IUserConnections<T>> _userConnections =
            new ConcurrentDictionary<T, IUserConnections<T>>();

        public virtual IUserConnections<T> GetIdentity(T userId)
        {
            return _userConnections.TryGetValue(userId, out var identity) ? identity : null;
        }
        public virtual IUserConnections<T> GetIdentity(IConnectionServer connection)
        {
            return _userConnections.Any(p => p.Value.Connections.Any(t => t != null && t.Websocket != null && t.Websocket.GetHashCode() == connection.Websocket.GetHashCode()))
               ? _userConnections.Values.FirstOrDefault(s => s.Connections.Any(t => t.Websocket.GetHashCode() == connection.Websocket.GetHashCode()))
               : (default);
        }
        public virtual IUserConnections<T>[] GetAllIdentities()
        {
            return _userConnections.Values.ToArray();
        }

        public virtual IUserConnections<T> AddUserConnection(T userId, IConnectionServer connection)
        {
            if (!_userConnections.TryGetValue(userId, out var userConnection))
            {
                userConnection = new UserConnections<T>
                {
                    Connections = new List<IConnectionServer>(),
                    Id = userId
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
        public virtual async Task RemoveUserConnectionAsync(IConnectionServer connection, bool disconnectConnection)
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
                        _userConnections.TryRemove(userConnection.Id, out userConnection);
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

        public virtual bool IsConnectionAuthorized(IConnectionServer connection)
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
        public virtual bool IsUserConnected(T userId)
        {
            return _userConnections.ContainsKey(userId);
        }
    }
}
