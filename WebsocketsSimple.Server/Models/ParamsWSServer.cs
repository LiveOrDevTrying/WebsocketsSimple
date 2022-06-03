using PHS.Networking.Models;
using System;

namespace WebsocketsSimple.Server.Models
{
    public class ParamsWSServer : ParamsPort
    {
        public string ConnectionSuccessString { get; protected set; }
        public string[] AvailableSubprotocols { get; protected set; }
        public int PingIntervalSec { get; protected set; }
        public bool OnlyEmitBytes { get; protected set; }

        public ParamsWSServer(int port, string connectionSuccessString = null, string[] availableSubprotocols = null, int pingIntervalSec = 30, bool onlyEmitBytes = false) : base(port)
        {
            if (port <= 0)
            {
                throw new ArgumentException("Ping must be greater than 0");
            }

            if (pingIntervalSec <= 0)
            {
                throw new ArgumentException("Ping must be greater than 0");
            }

            if (onlyEmitBytes && !string.IsNullOrWhiteSpace(connectionSuccessString))
            {
                throw new ArgumentException("onlyEmitBytes can not be true is a connectionSuccesString is specified");
            }

            ConnectionSuccessString = connectionSuccessString;
            AvailableSubprotocols = availableSubprotocols;
            PingIntervalSec = pingIntervalSec;
            OnlyEmitBytes = onlyEmitBytes;
        }
    }
}
