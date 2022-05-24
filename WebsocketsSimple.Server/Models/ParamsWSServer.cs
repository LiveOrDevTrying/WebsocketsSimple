namespace WebsocketsSimple.Server.Models
{
    public class ParamsWSServer
    {
        public int Port { get; set; }
        public string ConnectionSuccessString { get; set; }
        public string[] AvailableSubprotocols { get; set; }

        public int PingIntervalSec { get; set; } = 15;
        public int MaxPingAttempts { get; set; } = 1;
    }
}
