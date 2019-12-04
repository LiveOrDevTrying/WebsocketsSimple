using System.Threading.Tasks;
using PHS.Core.Models;
using WebsocketsSimple.Core.Events.Args;

namespace WebsocketsSimple.Client
{
    public interface IWebsocketClient : ICoreNetworking<WSConnectionEventArgs, WSMessageEventArgs, WSErrorEventArgs>
    {
        bool IsRunning { get; }

        Task<bool> Send(PacketDTO packet);
        Task<bool> Send(string message);
        Task<bool> Start(string url, int port, string parameters, bool isWSS);
        Task<bool> StopAsync();
    }
}