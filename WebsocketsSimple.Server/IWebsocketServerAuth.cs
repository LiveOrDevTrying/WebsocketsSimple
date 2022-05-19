using PHS.Networking.Events;
using PHS.Networking.Server.Events.Args;
using PHS.Networking.Services;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using WebsocketsSimple.Server.Events.Args;
using WebsocketsSimple.Server.Models;

namespace WebsocketsSimple.Server
{
    public interface IWebsocketServerAuth<T> : ICoreNetworking<WSConnectionServerAuthEventArgs<T>, WSMessageServerAuthEventArgs<T>, WSErrorServerAuthEventArgs<T>>
    {
        event NetworkingEventHandler<ServerEventArgs> ServerEvent;
        
        void Start(CancellationToken cancellationToken = default);
        void Stop();

        Task BroadcastToAllAuthorizedUsersAsync(string message, IConnectionWSServer connectionSending = null, CancellationToken cancellationToken = default);
        Task BroadcastToAllAuthorizedUsersAsync(byte[] message, IConnectionWSServer connectionSending = null, CancellationToken cancellationToken = default);

        Task<bool> SendToConnectionAsync(string message, IConnectionWSServer connectionSending = null, CancellationToken cancellationToken = default);
        Task<bool> SendToConnectionAsync(byte[] message, IConnectionWSServer connectionSending = null, CancellationToken cancellationToken = default);

        Task SendToUserAsync(string message, T userId, IConnectionWSServer connectionSending = null, CancellationToken cancellationToken = default);
        Task SendToUserAsync(byte[] message, T userId, IConnectionWSServer connectionSending = null, CancellationToken cancellationToken = default);

        Task<bool> DisconnectConnectionAsync(IConnectionWSServer connection, CancellationToken cancellationToken = default);

        IConnectionWSServer[] Connections { get; }
        IIdentityWS<T>[] Identities { get; }
        bool IsServerRunning { get; }
        TcpListener Server { get; }
    }
}