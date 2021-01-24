namespace WebsocketsSimple.Server.Models
{
    public class ParamsWSServerAuth : ParamsWSServer, IParamsWSServerAuth
    {
        public string ConnectionUnauthorizedString { get; set; }
    }
}
