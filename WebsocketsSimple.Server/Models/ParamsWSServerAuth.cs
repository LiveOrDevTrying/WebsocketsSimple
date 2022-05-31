namespace WebsocketsSimple.Server.Models
{
    public class ParamsWSServerAuth : ParamsWSServer
    {
        public ParamsWSServerAuth(int port, string connectionSuccessString = null, string connectionUnauthorizedString = null, string[] availableSubprotocols = null, int pingIntervalSec = 30) 
            : base(port, connectionSuccessString, availableSubprotocols, pingIntervalSec)
        {
            ConnectionUnauthorizedString = connectionUnauthorizedString;
        }

        public string ConnectionUnauthorizedString { get; protected set; }

    }
}
