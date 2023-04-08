using System.Collections.Generic;
using WebsocketsSimple.Core.Events.Args;
using WebsocketsSimple.Server.Models;

namespace WebsocketsSimple.Server.Events.Args
{
    public class WSConnectionServerBaseEventArgs<T> : WSConnectionEventArgs<T> where T : ConnectionWSServer
    {
        public Dictionary<string, string> RequestHeaders { get; set; }
    }
}

