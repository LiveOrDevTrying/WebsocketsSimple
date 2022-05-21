using PHS.Networking.Events;
using PHS.Networking.Server.Events.Args;
using PHS.Networking.Services;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using WebsocketsSimple.Core.Events.Args;
using WebsocketsSimple.Server.Models;

namespace WebsocketsSimple.Server
{
    public interface IWebsocketServerBase<T, U, V, W> : ICoreNetworking<T, U, V>
        where T : WSConnectionEventArgs<W>
        where U : WSMessageEventArgs<W>
        where V : WSErrorEventArgs<W>
        where W : ConnectionWSServer
    {
        event NetworkingEventHandler<ServerEventArgs> ServerEvent;
        
        void Start(CancellationToken cancellationToken = default);
        void Stop();

        Task<bool> BroadcastToAllConnectionsAsync(string message, W connectionSending = null, CancellationToken cancellationToken = default);
        Task<bool> BroadcastToAllConnectionsAsync(byte[] message, W connectionSending = null, CancellationToken cancellationToken = default);

        Task<bool> SendToConnectionAsync(string message, W connection, CancellationToken cancellationToken = default);
        Task<bool> SendToConnectionAsync(byte[] message, W connection, CancellationToken cancellationToken = default);

        Task DisconnectConnectionAsync(W connection, CancellationToken cancellationToken = default);

        IEnumerable<W> Connections { get; }
        int ConnectionCount { get; }
        bool IsServerRunning { get; }
        TcpListener Server { get; }

    }
}