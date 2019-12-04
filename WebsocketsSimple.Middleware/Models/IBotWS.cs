using System;

namespace PHS.WS.Core.Server.Middleware.Models
{
    public interface IBotWS
    {
        Guid Id { get; set; }
        string OAuthToken { get; set; }
    }
}