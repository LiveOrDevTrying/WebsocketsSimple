using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using PHS.Networking.Server.Events.Args;
using WebsocketsSimple.Server.Events.Args;
using WebsocketsSimple.Server.Handlers;
using WebsocketsSimple.Server.Models;
using WebsocketsSimple.Server.Managers;
using System.Net.WebSockets;
using PHS.Networking.Server.Services;

namespace WebsocketsSimple.Server
{
    public abstract class WebsocketServerBase<T, U, V, W, X, Y, Z> :
        CoreNetworkingServer<T, U, V, W, X, Y, Z>,
        ICoreNetworkingServer<T, U, V, Z>
        where T : WSConnectionServerBaseEventArgs<Z>
        where U : WSMessageServerBaseEventArgs<Z>
        where V : WSErrorServerBaseEventArgs<Z>
        where W : ParamsWSServer
        where X : WebsocketHandlerBase<T, U, V, W, Z>
        where Y : WSConnectionManager<Z>
        where Z : ConnectionWSServer
    {
        public WebsocketServerBase(W parameters) : base(parameters)
        {
        }
        public WebsocketServerBase(W parameters,
            byte[] certificate,
            string certificatePassword) : base(parameters, certificate, certificatePassword)
        {
        }
 
        protected override void OnServerEvent(object sender, ServerEventArgs args)
        {
            FireEvent(this, args);
        }

        public virtual async Task<bool> DisconnectConnectionAsync(Z connection, WebSocketCloseStatus webSocketCloseStatus = WebSocketCloseStatus.NormalClosure, string statusDescription = "Disconnect", CancellationToken cancellationToken = default)
        {
            return await _handler.DisconnectConnectionAsync(connection, webSocketCloseStatus, statusDescription, cancellationToken);
        }

        public TcpListener Server
        {
            get
            {
                return _handler?.Server;
            }
        }
    }
}
