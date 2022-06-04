using PHS.Networking.Server.Managers;
using System.Collections.Concurrent;
using WebsocketsSimple.Server.Models;

namespace WebsocketsSimple.Server.Managers
{
    public class WSConnectionManagerBase<T> : ConnectionManager<T> where T : ConnectionWSServer
    {
        public WSConnectionManagerBase() { }
        public WSConnectionManagerBase(ConcurrentDictionary<string, T> connections) : base(connections) { }
    }
}
