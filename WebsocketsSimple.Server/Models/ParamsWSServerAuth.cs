using System;

namespace WebsocketsSimple.Server.Models
{
    public class ParamsWSServerAuth : ParamsWSServer
    {
        public string ConnectionUnauthorizedString { get; protected set; }

        public ParamsWSServerAuth(int port, string connectionSuccessString = null, string connectionUnauthorizedString = null, string[] availableSubprotocols = null, int pingIntervalSec = 30, bool onlyEmitBytes = false) 
            : base(port, connectionSuccessString, availableSubprotocols, pingIntervalSec, onlyEmitBytes)
        {
            if (onlyEmitBytes && !string.IsNullOrWhiteSpace(connectionUnauthorizedString))
            {
                throw new ArgumentException("onlyEmitBytes can not be true is a connectionUnauthorizedString is specified");
            }

            ConnectionUnauthorizedString = connectionUnauthorizedString;
        }
    }
}
