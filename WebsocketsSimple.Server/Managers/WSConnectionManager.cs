using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using WebsocketsSimple.Server.Models;

namespace WebsocketsSimple.Server.Managers
{
    public class WSConnectionManager<T> where T : ConnectionWSServer
    {
        protected ConcurrentDictionary<string, T> _connections =
           new ConcurrentDictionary<string, T>();

        public WSConnectionManager()
        {
        }

        public WSConnectionManager(IEnumerable<T> connections)
        {
            _connections = new ConcurrentDictionary<string, T>();
            foreach (var item in connections)
            {
                _connections.TryAdd(item.ConnectionId, item);
            }
        }

        public virtual IEnumerable<T> GetAll()
        {
            return _connections.Values;
        }
        public virtual T Get(string id)
        {
            return _connections.TryGetValue(id, out var connection) ? connection : default;
        }
        public virtual bool Remove(string id)
        {
            return _connections.TryRemove(id, out var _);
        }

        public virtual bool Add(string id, T connection)
        {
            return _connections.TryAdd(id, connection);
        }
        public virtual int Count()
        {
            return _connections.Skip(0).Count();
        }

        public virtual IEnumerable<T> GetPingedConnections(int maxPingAttempts)
        {
            return _connections
                .Select(x => x.Value)
                .Where(x => x.PingAttempts >= maxPingAttempts);
        }
        public virtual IEnumerable<T> GetPingableConnections(int maxPingAttempts)
        {
            var ts = DateTime.UtcNow;
            return _connections
                .Select(x => x.Value)
                .Where(x => x.Websocket != null && x.PingAttempts < maxPingAttempts);
        }
    }
}
