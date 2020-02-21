namespace WebsocketsSimple.Server.Events.Args
{
    public class WSErrorServerAuthEventArgs<T> : WSErrorServerEventArgs
    {
        public T UserId { get; set; }
    }
}
