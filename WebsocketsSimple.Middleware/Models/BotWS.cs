using System;

namespace WebsocketsSimple.Middleware.Models
{
    public class BotWS : IBotWS
    {
        public Guid Id { get; set; }

        public string OAuthToken { get; set; }
    }
}
