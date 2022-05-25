using WebsocketsSimple.Core.Events.Args;
using WebsocketsSimple.Server.Models;

namespace WebsocketsSimple.Server.Events.Args
{
    public class WSMessageServerBaseEventArgs<T> : WSMessageEventArgs<T> where T : ConnectionWSServer
    {
    }
}
