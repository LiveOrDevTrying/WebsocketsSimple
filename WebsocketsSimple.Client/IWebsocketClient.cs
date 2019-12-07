using System.Threading.Tasks;
using PHS.Core.Models;
using WebsocketsSimple.Core.Events.Args;

namespace WebsocketsSimple.Client
{
    public interface IWebsocketClient : ICoreNetworking<WSConnectionEventArgs, WSMessageEventArgs, WSErrorEventArgs>
    {
        bool IsConnected { get; }

        Task<bool> SendAsync(PacketDTO packet);
        Task<bool> SendAsync(string message);
        Task<bool> StartAsync(string url, int port, string parameters, bool isWSS);
        Task<bool> StopAsync();
    }
}