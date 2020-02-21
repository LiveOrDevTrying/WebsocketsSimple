using System.Threading.Tasks;
using PHS.Networking.Models;
using PHS.Networking.Services;
using WebsocketsSimple.Client.Events.Args;
using WebsocketsSimple.Core.Models;

namespace WebsocketsSimple.Client
{
    public interface IWebsocketClient : ICoreNetworking<WSConnectionClientEventArgs, WSMessageClientEventArgs, WSErrorClientEventArgs>
    {
        Task<bool> SentToServerAsync<T>(T packet) where T : IPacket;
        Task<bool> SendToServerAsync(string message);
        Task<bool> SendToServerRawAsync(string message);

        Task<bool> ConnectAsync();
        Task<bool> DisconnectAsync();

        bool IsRunning { get; }
        IConnection Connection { get; }
    }
}