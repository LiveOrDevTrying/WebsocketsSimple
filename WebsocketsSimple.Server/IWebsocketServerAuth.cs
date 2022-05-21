using System.Threading;
using System.Threading.Tasks;
using WebsocketsSimple.Server.Events.Args;
using WebsocketsSimple.Server.Models;

namespace WebsocketsSimple.Server
{
    public interface IWebsocketServerAuth<T> : IWebsocketServerBase<WSConnectionServerEventArgs<IdentityWSServer<T>>, WSMessageServerEventArgs<IdentityWSServer<T>>, WSErrorServerEventArgs<IdentityWSServer<T>>, IdentityWSServer<T>>
    { 
        Task<bool> SendToUserAsync(string message, T userId, IdentityWSServer<T> connectionSending = null, CancellationToken cancellationToken = default);
        Task<bool> SendToUserAsync(byte[] message, T userId, IdentityWSServer<T> connectionSending = null, CancellationToken cancellationToken = default);
    }
}