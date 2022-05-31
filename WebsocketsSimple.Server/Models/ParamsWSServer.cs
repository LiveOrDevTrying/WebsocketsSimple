using PHS.Networking.Models;
using System;

namespace WebsocketsSimple.Server.Models
{
    public class ParamsWSServer : ParamsPort
    {
        public string ConnectionSuccessString { get; protected set; }
        public string[] AvailableSubprotocols { get; protected set; }
        public int PingIntervalSec { get; protected set; }

        public ParamsWSServer(int port, string connectionSuccessString = null, string[] availableSubprotocols = null, int pingIntervalSec = 30) : base(port)
        {
            ConnectionSuccessString = connectionSuccessString;
            AvailableSubprotocols = availableSubprotocols;
            PingIntervalSec = pingIntervalSec;
        }
    }
}
