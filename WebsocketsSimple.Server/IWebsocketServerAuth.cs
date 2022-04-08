using PHS.Networking.Events;
using PHS.Networking.Models;
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
        bool IsServerRunning { get; }
        TcpListener Server { get; }
        
        void Start(CancellationToken cancellationToken = default);
        void Stop();

        Task BroadcastToAllAuthorizedUsersAsync(string message);
        Task BroadcastToAllAuthorizedUsersAsync(string message, IConnectionWSServer connectionSending);
        Task BroadcastToAllAuthorizedUsersAsync<S>(S packet) where S : IPacket;
        Task BroadcastToAllAuthorizedUsersAsync<S>(S packet, IConnectionWSServer connectionSending) where S : IPacket;
        Task BroadcastToAllAuthorizedUsersRawAsync(string message);
        Task<bool> SendToConnectionAsync(string message, IConnectionWSServer connection);
        Task<bool> SendToConnectionAsync<S>(S packet, IConnectionWSServer connection) where S : IPacket;
        Task<bool> SendToConnectionRawAsync(string message, IConnectionWSServer connection);
        Task SendToUserAsync(string message, T userId);
        Task SendToUserAsync<S>(S packet, T userId) where S : IPacket;
        Task SendToUserRawAsync(string message, T userId);
        Task<bool> DisconnectConnectionAsync(IConnectionWSServer connection);

        IConnectionWSServer[] Connections { get; }
        IIdentityWS<T>[] Identities { get; }

        event NetworkingEventHandler<ServerEventArgs> ServerEvent;
    }
}