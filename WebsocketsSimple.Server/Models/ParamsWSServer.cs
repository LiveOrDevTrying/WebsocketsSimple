namespace PHS.Tcp.Core.Async.Server.Models
{
    public struct ParamsWSServer
    {
        public int PingIntervalSec { get; set; }
        public string ConnectionSuccessString { get; set; }
        public string UnauthorizedString { get; set; }
    }
}
