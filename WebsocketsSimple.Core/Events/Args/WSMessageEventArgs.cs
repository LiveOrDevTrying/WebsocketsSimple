using PHS.Networking.Events.Args;
using WebsocketsSimple.Core.Models;

namespace WebsocketsSimple.Core.Events.Args
{
    public class WSMessageEventArgs<T> : MessageEventArgs<T> where T : ConnectionWS
    {
        public string Message { get; set; }
    }
}
