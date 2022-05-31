using PHS.Networking.Events.Args;
using WebsocketsSimple.Core.Models;

namespace WebsocketsSimple.Core.Events.Args
{
    public class WSConnectionEventArgs<T> : ConnectionEventArgs<T> where T : ConnectionWS
    {
    }
}
