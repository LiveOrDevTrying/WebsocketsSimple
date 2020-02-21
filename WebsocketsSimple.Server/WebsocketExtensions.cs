using Microsoft.AspNetCore.Builder;
using WebsocketsSimple.Server.Middleware;

namespace WebsocketsSimple.Server
{
    public static class WebsocketExtensions
    {
        public static IApplicationBuilder MapWebSocketManager(this IApplicationBuilder app,
            IWebsocketServer server)
        {
            return app.Map("", (_app) => _app.UseMiddleware<WebsocketManagerMiddleware>(server));
        }

        public static IApplicationBuilder MapWebSocketManager<T>(this IApplicationBuilder app,
            IWebsocketServerAuth<T> server)
        {
            return app.Map("", (_app) => _app.UseMiddleware<WebsocketManagerMiddlewareAuth<T>>(server));
        }
    }
}
