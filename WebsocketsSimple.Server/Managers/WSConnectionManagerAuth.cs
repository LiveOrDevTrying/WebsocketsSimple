using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using WebsocketsSimple.Server.Models;

namespace WebsocketsSimple.Server.Managers
{
    public class WSConnectionManagerAuth<T> : WSConnectionManager
    {
        protected ConcurrentDictionary<T, IIdentityWS<T>> _identities =
            new ConcurrentDictionary<T, IIdentityWS<T>>();

        public IIdentityWS<T> GetIdentity(T userId)
        {
            return _identities.TryGetValue(userId, out var clientAuthorized) ? clientAuthorized : (default);
        }
        public IIdentityWS<T> GetIdentity(IConnectionWSServer connection)
        {
            return _identities.Any(p => p.Value.Connections.Any(t => t != null && t.Websocket.GetHashCode() == connection.Websocket.GetHashCode()))
                ? _identities.Values.FirstOrDefault(s => s.Connections.Any(t => t != null && t.Websocket.GetHashCode() == connection.Websocket.GetHashCode()))
                : (default);
        }
        public IIdentityWS<T>[] GetAllIdentities()
        {
            return _identities.Values.Where(s => s != null).ToArray();
        }

        public IIdentityWS<T> AddIdentity(T userId, IConnectionWSServer connection)
        {
            if (!_identities.TryGetValue(userId, out var instance))
            {
                instance = new IdentityWS<T>
                {
                    UserId = userId,
                    Connections = new List<IConnectionWSServer>()
                };
                _identities.TryAdd(userId, instance);
            }

            if (!instance.Connections.Any(s => s != null && s.Websocket.GetHashCode() == instance.GetHashCode()))
            {
                instance.Connections.Add(connection);
                return instance;
            }

            return null;
        }
        public void RemoveIdentity(IConnectionWSServer connection)
        {
            var udebtuty = _identities.Values.FirstOrDefault(s => s.Connections.Any(t => t != null && t.Websocket.GetHashCode() == connection.Websocket.GetHashCode()));

            if (udebtuty != null)
            {
                var instance = udebtuty.Connections.FirstOrDefault(s => s != null && s.Websocket.GetHashCode() == connection.Websocket.GetHashCode());

                if (instance != null)
                {
                    udebtuty.Connections.Remove(instance);

                    if (!udebtuty.Connections.Where(s => s != null).Any())
                    {
                        _identities.TryRemove(udebtuty.UserId, out udebtuty);
                    }
                }
            }
        }

        public bool IsConnectionAuthorized(IConnectionWSServer connection)
        {
            return _identities.Values.Any(s => s.Connections.Any(t => t != null && t.Websocket.GetHashCode() == connection.Websocket.GetHashCode()));
        }
        public bool IsUserConnected(T userId)
        {
            return _identities.ContainsKey(userId);
        }
    }
}
