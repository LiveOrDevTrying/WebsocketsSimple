using PHS.Networking.Server.Models;

namespace WebsocketsSimple.Server.Models
{
    public class IdentityWSServer<T> : ConnectionWSServer, IIdentity<T>
    {
        public T UserId { get; set; }
    }
}
