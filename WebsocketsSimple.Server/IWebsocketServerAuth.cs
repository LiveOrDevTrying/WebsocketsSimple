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
        Task BroadcastToAllUsersAsync<S>(S packet, IConnectionServer connectionSending) where S : IPacket;
        Task BroadcastToAllUsersAsync(string message, IConnectionServer connectionSending);
        Task BroadcastToAllUsersRawAsync(string message);
        Task SendToUserAsync<S>(S packet, T userId) where S : IPacket;
        Task SendToUserAsync(string message, T userId);
        Task SendToUserRawAsync(string message, T userId);

        Task<bool> SendToConnectionAsync<S>(S packet, IConnectionServer connection) where S : IPacket;
        Task<bool> SendToConnectionAsync(string message, IConnectionServer connection);
        Task<bool> SendToConnectionRawAsync(string message, IConnectionServer connection);
        Task DisconnectConnectionAsync(IConnectionServer connection);

        Task AuthorizeAndStartReceivingAsync(IConnectionServer connection, string oauthToken);
        
        IConnectionServer[] Connections { get; }
        IUserConnections<T>[] UserConnections { get; }
        WebsocketConnectionManagerAuth<T> ConnectionManager { get; }
    }
}