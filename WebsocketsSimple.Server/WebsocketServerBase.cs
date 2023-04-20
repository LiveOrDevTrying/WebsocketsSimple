using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using WebsocketsSimple.Server.Events.Args;
using WebsocketsSimple.Server.Handlers;
using WebsocketsSimple.Server.Models;
using WebsocketsSimple.Server.Managers;
using System.Net.WebSockets;
using PHS.Networking.Server.Services;
using System.Linq;

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
        where Y : WSConnectionManagerBase<Z>
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

        public virtual async Task<bool> DisconnectConnectionAsync(Z connection, WebSocketCloseStatus webSocketCloseStatus = WebSocketCloseStatus.NormalClosure, string statusDescription = "Disconnect", CancellationToken cancellationToken = default)
        {
            return await _handler.DisconnectConnectionAsync(connection, webSocketCloseStatus, statusDescription, cancellationToken);
        }

        public virtual async Task SendToChannelAsync(string message, string channel, CancellationToken cancellationToken = default)
        {
            foreach (var connection in _connectionManager.GetAll().Where(x => x.Channel.Trim().ToLower() == channel.Trim().ToLower()))
            {
                await _handler.SendAsync(message, connection, cancellationToken);
            }
        }
        public virtual async Task SendToChannelAsync(byte[] message, string channel, CancellationToken cancellationToken = default)
        {
            foreach (var connection in _connectionManager.GetAll().Where(x => x.Channel.Trim().ToLower() == channel.Trim().ToLower()))
            {
                await _handler.SendAsync(message, connection, cancellationToken);
            }
        }

        protected abstract T CreateConnectionEventArgs(WSConnectionServerBaseEventArgs<Z> args);
        protected abstract U CreateMessageEventArgs(WSMessageServerBaseEventArgs<Z> args);
        protected abstract V CreateErrorEventArgs(WSErrorServerBaseEventArgs<Z> args);

        public TcpListener Server
        {
            get
            {
                return _handler?.Server;
            }
        }
    }
}
