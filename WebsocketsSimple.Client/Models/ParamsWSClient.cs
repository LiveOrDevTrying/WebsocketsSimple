using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace WebsocketsSimple.Client.Models
{
    public class ParamsWSClient : IParamsWSClient
    {
        public string Uri { get; set; }
        public int Port { get; set; }
        public bool IsWebsocketSecured { get; set; }
        public IDictionary<string, string> RequestHeaders { get; set; }
        public string[] RequestedSubProtocols { get; set; }
        public X509Certificate2[] ClientCertificates { get; set; }
        public SslProtocols EnabledSslProtocols { get; set; } = SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12;
        public TimeSpan KeepAliveInterval { get; set; } = WebSocket.DefaultKeepAliveInterval;
        public int ReceiveBufferSize { get; set; } = 2048;
        public int SendBufferSize { get; set; } = 2048;
    }
}
