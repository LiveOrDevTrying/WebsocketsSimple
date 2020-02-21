using PHS.Networking.Events.Args;
using WebsocketsSimple.Core.Models;

namespace WebsocketsSimple.Core.Events.Args
{
    public class WSConnectionEventArgs<T> : ConnectionEventArgs where T : IConnection
    {
        public T Connection { get; set; }
    }
}
