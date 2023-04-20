using PHS.Networking.Server.Services;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using WebsocketsSimple.Server.Events.Args;
using WebsocketsSimple.Server.Models;

namespace WebsocketsSimple.Server
{
    public interface IWebsocketServer : 
        ICoreNetworkingServer<
            WSConnectionServerEventArgs, 
            WSMessageServerEventArgs, 
            WSErrorServerEventArgs, 
            ConnectionWSServer>
    {
        Task SendToChannelAsync(string message, string channel, CancellationToken cancellationToken = default);
        Task SendToChannelAsync(byte[] message, string channel, CancellationToken cancellationToken = default);

        Task<bool> DisconnectConnectionAsync(ConnectionWSServer connection,
            WebSocketCloseStatus webSocketCloseStatus = WebSocketCloseStatus.NormalClosure,
            string statusDescription = "Disconnect",
            CancellationToken cancellationToken = default);

        TcpListener Server { get; }
    }
}