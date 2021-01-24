namespace WebsocketsSimple.Server.Models
{
    public interface IParamsWSServerAuth : IParamsWSServer
    {
        string ConnectionUnauthorizedString { get; set; }
    }
}