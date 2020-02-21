namespace WebsocketsSimple.Client.Models
{
    public interface IParamsWSClient
    {
        string Uri { get; set; }
        int Port { get; set; }
        bool IsWebsocketSecured { get; set; }
    }
}
