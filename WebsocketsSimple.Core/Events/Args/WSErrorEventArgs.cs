using PHS.Networking.Events.Args;
using WebsocketsSimple.Core.Models;

namespace WebsocketsSimple.Core.Events.Args
{
    public class WSErrorEventArgs<T> : ErrorEventArgs<T> where T : ConnectionWS
    {
    }
}
