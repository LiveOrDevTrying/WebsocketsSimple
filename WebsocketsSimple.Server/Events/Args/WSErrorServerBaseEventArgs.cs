using WebsocketsSimple.Core.Events.Args;
using WebsocketsSimple.Server.Models;

namespace WebsocketsSimple.Server.Events.Args
{
    public class WSErrorServerBaseEventArgs<T> : WSErrorEventArgs<T> where T : ConnectionWSServer
    {
    }
}
