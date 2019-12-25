using System;

namespace WebsocketsSimple.Middleware.Models
{
    public interface IBotWS
    {
        Guid Id { get; set; }
        string OAuthToken { get; set; }
    }
}