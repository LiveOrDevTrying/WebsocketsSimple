using PHS.Networking.Events.Args;
using PHS.Networking.Models;
using WebsocketsSimple.Core.Models;

namespace WebsocketsSimple.Core.Events.Args
{
    public class WSMessageEventArgs<T>: MessageEventArgs where T : IConnection
    {
        public T Connection { get; set; }
    }
}
