using System.Threading.Tasks;
using PHS.Core.Models;
using PHS.Core.Networking;
using WebsocketsSimple.Core.Events.Args;

namespace WebsocketsSimple.Client
{
    public interface IWebsocketClient : ICoreNetworking<WSConnectionEventArgs, WSMessageEventArgs, WSErrorEventArgs>, 
        INetworkClient
    {
        Task<bool> SendAsync(PacketDTO packet);
        Task<bool> SendAsync(string message);
        Task<bool> ConnectAsync(string url, int port, string parameters, bool isWSS);
        Task<bool> DisconnectAsync();
    }
}