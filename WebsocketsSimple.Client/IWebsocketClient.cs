using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using PHS.Networking.Models;
using PHS.Networking.Services;
using WebsocketsSimple.Client.Events.Args;
using WebsocketsSimple.Core.Models;

namespace WebsocketsSimple.Client
{
    public interface IWebsocketClient : ICoreNetworking<WSConnectionClientEventArgs, WSMessageClientEventArgs, WSErrorClientEventArgs>
    {
        Task<bool> SendAsync(string message, CancellationToken cancellationToken = default);
        Task<bool> SendAsync(byte[] message, CancellationToken cancellationToken = default);

        Task<bool> ConnectAsync(CancellationToken cancellationToken = default);
        Task<bool> DisconnectAsync(WebSocketCloseStatus webSocketCloseStatus = WebSocketCloseStatus.NormalClosure,
            string closeStatusDescription = "Disconnect",
            CancellationToken cancellationToken = default);

        bool IsRunning { get; }
        ConnectionWS Connection { get; }
    }
}