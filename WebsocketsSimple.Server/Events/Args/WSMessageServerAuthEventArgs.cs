namespace WebsocketsSimple.Server.Events.Args
{
    public class WSMessageServerAuthEventArgs<T> : WSMessageServerEventArgs
    {
        public T UserId { get; set; }
    }
}
