using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using PHS.Networking.Services;
using WebsocketsSimple.Client.Events.Args;
using WebsocketsSimple.Core.Models;

namespace WebsocketsSimple.Client
{
    public interface IWebsocketClient : 
        ICoreNetworkingClient<
            WSConnectionClientEventArgs, 
            WSMessageClientEventArgs,
            WSErrorClientEventArgs,
            ConnectionWS>
    {
        Task<bool> DisconnectAsync(WebSocketCloseStatus webSocketCloseStatus = WebSocketCloseStatus.NormalClosure,
            string closeStatusDescription = "Disconnect",
            CancellationToken cancellationToken = default);
    }
}