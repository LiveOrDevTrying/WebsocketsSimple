namespace WebsocketsSimple.Server.Models
{
    public class ParamsWSServer
    {
        public int Port { get; set; }
        public string ConnectionSuccessString { get; set; }
        public string[] AvailableSubprotocols { get; set; }

        public int MaxConnectionsPingedPerInterval { get; set; } = 2000;
        public int PingIntervalSec { get; set; } = 15;
    }
}
