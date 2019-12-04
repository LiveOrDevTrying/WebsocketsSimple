using Microsoft.AspNetCore.Http;
using PHS.WS.Core.Server.Middleware.Models;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebsocketsSimple.Server;

namespace PHS.WS.Core.Server.Middleware
{
    public class WebsocketManagerMiddleware
    {
        protected readonly RequestDelegate _next;
        protected readonly WebsocketManagerParams _parameters;
        protected readonly IWebsocketServer _websocketServer;
        protected readonly IUserServiceWebsocket _userService;

        public WebsocketManagerMiddleware(RequestDelegate next,
            WebsocketManagerParams parameters,
            IWebsocketServer websocketServer,
            IUserServiceWebsocket userService)
        {
            _next = next;
            _parameters = parameters;
            _websocketServer = websocketServer;
            _userService = userService;
        }

        public virtual async Task Invoke(HttpContext context)
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                return;
            }

            var token = context.Request.Path.ToString().Substring(1);
            var websocket = await context.WebSockets.AcceptWebSocketAsync();

            if (string.IsNullOrEmpty(token))
            {
                await _websocketServer.SendToWebsocketRawAsync(_parameters.InvalidTokenString, websocket);
                await _websocketServer.DisconnectClientAsync(websocket);
                websocket.Dispose();
                return;
            }

            var bot = await _userService.GetWSBotAsync(token);

            if (bot == null)
            {
                await _websocketServer.SendToWebsocketRawAsync(_parameters.UnauthorizedString, websocket);
                await _websocketServer.DisconnectClientAsync(websocket);
                websocket.Dispose();
                return;
            }

            // R8D: This should include Bot? 
            //_websocketServer.ConnectClient(bot.User.Id, websocket, bot);
            _websocketServer.ConnectClient(bot.Id, websocket);

            await _websocketServer.SendToWebsocketRawAsync(_parameters.AuthorizedString, websocket);

            // To do: Add in completed ev ents

            await Receive(websocket, async(result, message) =>
            {
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    await _websocketServer.ReceiveAsync(websocket, result, message);
                    return;
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    await _websocketServer.DisconnectClientAsync(websocket);
                    return;
                }
            });

            //TODO - investigate the Kestrel exception thrown when this is the last middleware
            await _next.Invoke(context);
        }

        private async Task Receive(WebSocket socket, Action<WebSocketReceiveResult, string> handleMessage)
        {
            var buffer = new byte[1024 * 4];

            while (socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(buffer: new ArraySegment<byte>(buffer),
                                                       cancellationToken: CancellationToken.None);

                handleMessage(result, Encoding.UTF8.GetString(buffer, 0, result.Count));
            }
        }
    }
}
