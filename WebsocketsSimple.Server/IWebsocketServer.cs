using PHS.Networking.Events;
using PHS.Networking.Models;
using PHS.Networking.Server.Events.Args;
using PHS.Networking.Services;
using System.Net.Sockets;
using System.Threading.Tasks;
using WebsocketsSimple.Server.Events.Args;
using WebsocketsSimple.Server.Models;

namespace WebsocketsSimple.Server
{
    public interface IWebsocketServer : ICoreNetworking<WSConnectionServerEventArgs, WSMessageServerEventArgs, WSErrorServerEventArgs>
    {
        bool IsServerRunning { get; }
        TcpListener Server { get; }

        Task StartAsync();
        Task StopAsync();

        Task<bool> SendToConnectionAsync<T>(T packet, IConnectionWSServer connection) where T : IPacket;
        Task<bool> SendToConnectionAsync(string message, IConnectionWSServer connection);
        Task<bool> SendToConnectionRawAsync(string message, IConnectionWSServer connection);
        Task<bool> DisconnectConnectionAsync(IConnectionWSServer connection);

        IConnectionWSServer[] Connections { get; }

        event NetworkingEventHandler<ServerEventArgs> ServerEvent;
    }
}