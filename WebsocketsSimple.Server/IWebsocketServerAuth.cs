using System.Threading.Tasks;
using PHS.Networking.Models;
using PHS.Networking.Services;
using WebsocketsSimple.Server.Events.Args;
using WebsocketsSimple.Server.Managers;
using WebsocketsSimple.Server.Models;

namespace WebsocketsSimple.Server
{
    public interface IWebsocketServerAuth<T> : ICoreNetworking<WSConnectionServerAuthEventArgs<T>, WSMessageServerAuthEventArgs<T>, WSErrorServerAuthEventArgs<T>>
    {
        Task BroadcastToAllUsersAsync<S>(S packet) where S : IPacket;
        Task BroadcastToAllUsersAsync(string message);
        Task BroadcastToAllUsersAsync<S>(S packet, IConnectionWSServer connectionSending) where S : IPacket;
        Task BroadcastToAllUsersAsync(string message, IConnectionWSServer connectionSending);
        Task BroadcastToAllUsersRawAsync(string message);
        Task SendToUserAsync<S>(S packet, T userId) where S : IPacket;
        Task SendToUserAsync(string message, T userId);
        Task SendToUserRawAsync(string message, T userId);

        Task<bool> SendToConnectionAsync<S>(S packet, IConnectionWSServer connection) where S : IPacket;
        Task<bool> SendToConnectionAsync(string message, IConnectionWSServer connection);
        Task<bool> SendToConnectionRawAsync(string message, IConnectionWSServer connection);
        Task DisconnectConnectionAsync(IConnectionWSServer connection);

        Task AuthorizeAndStartReceivingAsync(IConnectionWSServer connection, string oauthToken);
        
        IConnectionWSServer[] Connections { get; }
        IUserConnectionsWS<T>[] UserConnections { get; }
        WebsocketConnectionManagerAuth<T> ConnectionManager { get; }
    }
}