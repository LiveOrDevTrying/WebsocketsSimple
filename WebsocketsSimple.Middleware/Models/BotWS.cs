using System;

namespace PHS.WS.Core.Server.Middleware.Models
{
    public class BotWS : IBotWS
    {
        public Guid Id { get; set; }

        public string OAuthToken { get; set; }
    }
}
