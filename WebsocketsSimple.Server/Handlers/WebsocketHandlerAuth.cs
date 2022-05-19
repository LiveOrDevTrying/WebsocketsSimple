using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using WebsocketsSimple.Core;
using WebsocketsSimple.Server.Events.Args;
using WebsocketsSimple.Server.Models;

namespace WebsocketsSimple.Server.Handlers
{
    public delegate void WebsocketAuthorizeEvent(object sender, WSAuthorizeEventArgs args);

    public class WebsocketHandlerAuth : WebsocketHandler
    {
        private event WebsocketAuthorizeEvent _authorizeEvent;

        public WebsocketHandlerAuth(ParamsWSServer parameters) : base(parameters)
        {
        }
        public WebsocketHandlerAuth(ParamsWSServer parameters, byte[] certificate, string certificatePassword)
            : base(parameters, certificate, certificatePassword)
        {
        }

        protected override Task<bool> UpgradeConnectionAsync(string message, IConnectionWSServer connection, CancellationToken cancellationToken)
        {
            // Checking auth token
            if (message.IndexOf("?") <= 0)
            {
                return Task.FromResult(false);
            }
            var qs = HttpUtility.ParseQueryString(message.Substring(message.IndexOf("?")));

            var token = qs.Get("token");

            if (string.IsNullOrWhiteSpace(token))
            {
                return Task.FromResult(false);
            }

            FireEvent(this, new WSAuthorizeEventArgs
            {
                Connection = connection,
                Token = token,
                UpgradeData = message
            });

            return Task.FromResult(true);
        }
        public virtual async Task<bool> UpgradeConnectionCallbackAsync(WSAuthorizeEventArgs args, CancellationToken cancellationToken)
        {
            // 1. Obtain the value of the "Sec-WebSocket-Key" request header without any leading or trailing whitespace
            // 2. Concatenate it with "258EAFA5-E914-47DA-95CA-C5AB0DC85B11" (a special GUID specified by RFC 6455)
            // 3. Compute SHA-1 and Base64 hash of the new value
            // 4. Write the hash back as the value of "Sec-WebSocket-Accept" response header in an HTTP response
            var swk = Regex.Match(args.UpgradeData, $"{HttpKnownHeaderNames.SecWebSocketKey}: (.*)").Groups[1].Value.Trim();
            var swka = swk + Statics.WS_SERVER_GUID;
            var swkaSha1 = System.Security.Cryptography.SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(swka));
            var swkaSha1Base64 = Convert.ToBase64String(swkaSha1);
            var requestedSubprotocols = Regex.Match(args.UpgradeData, $"{HttpKnownHeaderNames.SecWebSocketProtocol}: (.*)").Groups[1].Value.Trim().Split(",");

            if (requestedSubprotocols.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray().Length > 0)
            {
                if (!AreSubprotocolsRequestedValid(requestedSubprotocols))
                {
                    var bytes = Encoding.UTF8.GetBytes("Invalid subprotocols requested");
                    await args.Connection.Stream.WriteAsync(bytes, cancellationToken);
                    await DisconnectConnectionAsync(args.Connection, cancellationToken);
                    return false;
                }
            }

            var selectedSubProtocol = requestedSubprotocols.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray().Length > 0 ? $"{HttpKnownHeaderNames.SecWebSocketProtocol}: {requestedSubprotocols[0]}\r\n" : string.Empty;
            // HTTP/1.1 defines the sequence CR LF as the end-of-line marker
            var response = Encoding.UTF8.GetBytes(
                "HTTP/1.1 101 Switching Protocols\r\n" +
                $"{HttpKnownHeaderNames.Connection}: Upgrade\r\n" +
                $"{HttpKnownHeaderNames.Upgrade}: websocket\r\n" +
                $"{HttpKnownHeaderNames.SecWebSocketAccept}: {swkaSha1Base64}\r\n" +
                $"{selectedSubProtocol}\r\n");

            args.Connection.SubProtocols = requestedSubprotocols;
            await args.Connection.Stream.WriteAsync(response, 0, response.Length, cancellationToken);

            await SendAsync(_parameters.ConnectionSuccessString, args.Connection, cancellationToken);

            return true;
        }

        protected virtual void FireEvent(object sender, WSAuthorizeEventArgs args)
        {
            _authorizeEvent?.Invoke(sender, args);
        }

        public event WebsocketAuthorizeEvent AuthorizeEvent
        {
            add
            {
                _authorizeEvent += value;
            }
            remove
            {
                _authorizeEvent -= value;
            }
        }
    }
}