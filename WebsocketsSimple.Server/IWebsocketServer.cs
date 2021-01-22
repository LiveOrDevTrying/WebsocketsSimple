using System.Threading.Tasks;
using PHS.Networking.Models;
using PHS.Networking.Services;
using WebsocketsSimple.Server.Events.Args;
using WebsocketsSimple.Server.Managers;
using WebsocketsSimple.Server.Models;

namespace WebsocketsSimple.Server
{
    public interface IWebsocketServer : ICoreNetworking<WSConnectionServerEventArgs, WSMessageServerEventArgs, WSErrorServerEventArgs>
    {
        Task<bool> SendToConnectionAsync<S>(S packet, IConnectionWSServer connection) where S : IPacket;
        Task<bool> SendToConnectionAsync(string message, IConnectionWSServer connection);
        Task<bool> SendToConnectionRawAsync(string message, IConnectionWSServer connection);
        Task DisconnectConnectionAsync(IConnectionWSServer connection);

        Task StartReceivingAsync(IConnectionWSServer connection);
        
        IConnectionWSServer[] Connections { get; }
        WebsocketConnectionManager ConnectionManager { get; }
    }
}