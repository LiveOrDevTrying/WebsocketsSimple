namespace PHS.Tcp.Core.Async.Server.Models
{
    public interface IParamsWSServerAuth : IParamsWSServer
    {
        string ConnectionUnauthorizedString { get; set; }
    }
}