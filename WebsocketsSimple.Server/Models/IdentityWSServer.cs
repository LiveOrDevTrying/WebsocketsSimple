namespace WebsocketsSimple.Server.Models
{
    public class IdentityWSServer<T> : ConnectionWSServer
    {
        public T UserId { get; set; }
    }
}
