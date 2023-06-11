using PHS.Networking.Models;

namespace WebsocketsSimple.Server.Models
{
    public interface IParamsWSServer : IParams
    {
        string[] AvailableSubprotocols { get; }
        string ConnectionSuccessString { get; }
        bool OnlyEmitBytes { get; }
        int PingIntervalSec { get; }
        int Port { get; }
    }
}