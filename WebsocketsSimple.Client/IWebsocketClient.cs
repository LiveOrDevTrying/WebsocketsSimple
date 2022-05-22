using System.Threading;
using System.Threading.Tasks;
using PHS.Networking.Models;
using PHS.Networking.Services;
using WebsocketsSimple.Client.Events.Args;

namespace WebsocketsSimple.Client
{
    public interface IWebsocketClient : ICoreNetworking<WSConnectionClientEventArgs, WSMessageClientEventArgs, WSErrorClientEventArgs>
    {
        Task<bool> SendAsync(string message, CancellationToken cancellationToken = default);
        Task<bool> SendAsync(byte[] message, CancellationToken cancellationToken = default);

        Task<bool> ConnectAsync(CancellationToken cancellationToken = default);
        Task<bool> DisconnectAsync(CancellationToken cancellationToken = default);

        bool IsRunning { get; }
        IConnection Connection { get; }
    }
}