namespace PHS.Tcp.Core.Async.Server.Models
{
    public struct ParamsWSServerAuth : IParamsWSServerAuth
    {
        public string ConnectionSuccessString { get; set; }
        public string ConnectionUnauthorizedString { get; set; }
    }
}
