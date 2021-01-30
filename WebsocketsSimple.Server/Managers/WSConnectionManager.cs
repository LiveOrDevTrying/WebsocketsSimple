using System.Collections.Concurrent;
using System.Linq;
using System.Net.WebSockets;
using WebsocketsSimple.Server.Models;

namespace WebsocketsSimple.Server.Managers
{
    public class WSConnectionManager
    {
        protected ConcurrentDictionary<int, IConnectionWSServer> _connections =
            new ConcurrentDictionary<int, IConnectionWSServer>();

        public IConnectionWSServer[] GetAllConnections()
        {
            return _connections.Values.ToArray();
        }
        public IConnectionWSServer GetConnection(WebSocket websocket)
        {
            return _connections.TryGetValue(websocket.GetHashCode(), out var connection) ? connection : null;
        }
        public bool AddConnection(IConnectionWSServer connection)
        {
            return !_connections.ContainsKey(connection.Websocket.GetHashCode()) ? _connections.TryAdd(connection.Websocket.GetHashCode(), connection) : false;
        }
        public void RemoveConnection(IConnectionWSServer connection)
        {
            _connections.TryRemove(connection.Websocket.GetHashCode(), out var instance);
        }
        public bool IsConnectionOpen(IConnectionWSServer connection)
        {
            return _connections.TryGetValue(connection.Websocket.GetHashCode(), out var instance) ? instance.Websocket.State == WebSocketState.Open : false;
        }
    }
}
