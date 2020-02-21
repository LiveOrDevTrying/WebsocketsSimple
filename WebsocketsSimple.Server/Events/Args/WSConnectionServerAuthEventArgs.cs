namespace WebsocketsSimple.Server.Events.Args
{
    public class WSConnectionServerAuthEventArgs<T> : WSConnectionServerEventArgs
    {
        public T UserId { get; set; }
    }
}
