using PHS.Networking.Server.Services;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using WebsocketsSimple.Server.Events.Args;
using WebsocketsSimple.Server.Models;

namespace WebsocketsSimple.Server
{
    public interface IWebsocketServerAuth<T> :
        ICoreNetworkingServer<
            WSConnectionServerAuthEventArgs<T>, 
            WSMessageServerAuthEventArgs<T>, 
            WSErrorServerAuthEventArgs<T>, 
            IdentityWSServer<T>>
    { 
        Task<bool> SendToUserAsync(string message, T userId, CancellationToken cancellationToken = default);
        Task<bool> SendToUserAsync(byte[] message, T userId, CancellationToken cancellationToken = default);

        Task<bool> DisconnectConnectionAsync(IdentityWSServer<T> connection,
            WebSocketCloseStatus webSocketCloseStatus = WebSocketCloseStatus.NormalClosure,
            string statusDescription = "Disconnect",
            CancellationToken cancellationToken = default);

        TcpListener Server { get; }
    }
}