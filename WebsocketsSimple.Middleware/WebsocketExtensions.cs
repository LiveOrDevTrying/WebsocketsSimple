using Microsoft.AspNetCore.Builder;
using PHS.Core.Services;
using WebsocketsSimple.Middleware.Models;
using WebsocketsSimple.Server;

namespace WebsocketsSimple.Middleware
{
    public static class WebsocketExtensions
    {
        public static IApplicationBuilder MapWebSocketManager(this IApplicationBuilder app,
            WebsocketManagerParams parameters,
            IWebsocketServer server,
            IUserService userService)
        {
            return app.Map("", (_app) => _app.UseMiddleware<WebsocketManagerMiddleware>(parameters, server, userService));
        }
    }
}
