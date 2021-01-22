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
    public class WebsocketConnectionManager
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
        public async Task RemoveConnectionAsync(IConnectionWSServer connection, bool disconnectConnection)
        {
            if (_connections.TryRemove(connection.Websocket.GetHashCode(), out var instance) &&
                disconnectConnection &&
                instance.Websocket != null)
            {
                await instance.Websocket.CloseAsync(WebSocketCloseStatus.Empty, "Connection closed", CancellationToken.None);
                instance.Websocket.Dispose();
            }
        }
        public bool IsConnectionOpen(IConnectionWSServer connection)
        {
            return _connections.TryGetValue(connection.Websocket.GetHashCode(), out var instance) ? instance.Websocket.State == WebSocketState.Open : false;
        }
    }
}