namespace WebsocketsSimple.Server.Models
{ 
    public interface IParamsWSServer
    {
        int Port { get; }
        string ConnectionSuccessString { get; }
        string[] AvailableSubprotocols { get; }
    }
}