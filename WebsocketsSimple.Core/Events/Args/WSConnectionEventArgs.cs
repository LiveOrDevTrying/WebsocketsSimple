using PHS.Core.Events.Args.NetworkEventArgs;
using System.Net.WebSockets;

namespace WebsocketsSimple.Core.Events.Args
{
    public class WSConnectionEventArgs : ConnectionEventArgs
    {
        public WebSocket Websocket { get; set; }
    }
}
