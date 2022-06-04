using System.Collections.Concurrent;
using WebsocketsSimple.Server.Models;

namespace WebsocketsSimple.Server.Managers
{
    public class WSConnectionManager : WSConnectionManagerBase<ConnectionWSServer>
    {
        public WSConnectionManager() { }
        public WSConnectionManager(ConcurrentDictionary<string, ConnectionWSServer> connections) : base(connections) { }
    }
}
