namespace WebsocketsSimple.Server.Models
{
    public class ParamsWSServer : IParamsWSServer
    {
        public int Port { get; set; }
        public string ConnectionSuccessString { get; set; }
        public string[] AvailableSubprotocols { get; set; }
    }
}
