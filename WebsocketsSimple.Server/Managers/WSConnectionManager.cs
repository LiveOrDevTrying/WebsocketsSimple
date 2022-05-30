using PHS.Networking.Server.Managers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using WebsocketsSimple.Server.Models;

namespace WebsocketsSimple.Server.Managers
{
    public class WSConnectionManager<T> : ConnectionManager<T> where T : ConnectionWSServer
    {
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
    }
}
