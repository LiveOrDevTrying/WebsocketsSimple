namespace WebsocketsSimple.Middleware.Models
{
    public struct WebsocketManagerParams
    {
        public string InvalidTokenString { get; set; }
        public string UnauthorizedString { get; set; }
        public string AuthorizedString { get; set; }
    }
}
