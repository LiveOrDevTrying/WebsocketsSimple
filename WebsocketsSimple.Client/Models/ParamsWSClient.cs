namespace WebsocketsSimple.Client.Models
{
    public struct ParamsWSClient : IParamsWSClient
    {
        public string Uri { get; set; }
        public int Port { get; set; }
        public bool IsWebsocketSecured { get; set; }
    }
}
