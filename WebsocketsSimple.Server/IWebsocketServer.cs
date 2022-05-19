using PHS.Networking.Events;
using PHS.Networking.Models;
using PHS.Networking.Server.Events.Args;
using PHS.Networking.Services;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using WebsocketsSimple.Server.Events.Args;
using WebsocketsSimple.Server.Models;

namespace WebsocketsSimple.Server
{
    public interface IWebsocketServer : ICoreNetworking<WSConnectionServerEventArgs, WSMessageServerEventArgs, WSErrorServerEventArgs>
    {
        event NetworkingEventHandler<ServerEventArgs> ServerEvent;
        
        void Start(CancellationToken cancellationToken = default);
        void Stop();

        Task<bool> SendToConnectionAsync(string message, IConnectionWSServer connection, CancellationToken cancellationToken = default);
        Task<bool> SendToConnectionAsync(byte[] message, IConnectionWSServer connection, CancellationToken cancellationToken = default);

        Task<bool> DisconnectConnectionAsync(IConnectionWSServer connection, CancellationToken cancellationToken = default);

        IConnectionWSServer[] Connections { get; }
        bool IsServerRunning { get; }
        TcpListener Server { get; }

    }
}