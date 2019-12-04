using PHS.Core.Events.Args.NetworkEventArgs;
using System.Net.WebSockets;

namespace WebsocketsSimple.Core.Events.Args
{
    public class WSErrorEventArgs : ErrorEventArgs
    {
        public WebSocket Websocket { get; set; }
    }
}
