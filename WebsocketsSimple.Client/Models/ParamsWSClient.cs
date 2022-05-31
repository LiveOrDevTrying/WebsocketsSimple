using PHS.Networking.Models;
using System;
using System.Collections.Generic;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace WebsocketsSimple.Client.Models
{
    public class ParamsWSClient : Params
    {
        public string Host { get; protected set; }
        public int Port { get; protected set; }
        public string Path { get; protected set; }
        public KeyValuePair<string, string>[] QueryStringParameters { get; protected set; }
        public bool IsWebsocketSecured { get; protected set; }
        public IDictionary<string, string> RequestHeaders { get; protected set; }
        public string[] RequestedSubProtocols { get; protected set; }
        public X509Certificate2[] ClientCertificates { get; protected set; }
        public SslProtocols EnabledSslProtocols { get; protected set; }
        public TimeSpan KeepAliveInterval { get; protected set; }
        public int ReceiveBufferSize { get; protected set; }
        public int SendBufferSize { get; protected set; }
        public string Token { get; protected set; }

        public ParamsWSClient(string host, int port, bool isWebsocketSecured, string token = "", string path = "", KeyValuePair<string, string>[] queryStringParameters = null, IDictionary<string, string> requestHeaders = null,
           string[] requestedSubProtocols = null, X509Certificate2[] clientCertificates = null, SslProtocols enabledSSLProtocols = SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12,
           int keepAliveIntervalSec = 30, int receiveBufferSize = 2048, int sendBufferSize = 2048)
        {
            if (string.IsNullOrWhiteSpace(host))
            {
                throw new ArgumentException("Host is not valid");
            }

            if (port <= 0)
            {
                throw new ArgumentException("Port is not valid");
            }

            if (keepAliveIntervalSec <= 0)
            {
                throw new ArgumentException("KeepAliveIntervalSec is not valid");
            }

            if (receiveBufferSize <= 0)
            {
                throw new ArgumentException("ReceiveBufferSize is not valid");
            }

            if (sendBufferSize <= 0)
            {
                throw new ArgumentException("SendBufferSize is not valid");
            }

            Host = host;
            Port = port;
            IsWebsocketSecured = isWebsocketSecured;
            Token = token;
            Path = path;
            QueryStringParameters = queryStringParameters;
            RequestHeaders = requestHeaders;
            RequestedSubProtocols = requestedSubProtocols;
            ClientCertificates = clientCertificates;
            EnabledSslProtocols = enabledSSLProtocols;
            KeepAliveInterval = TimeSpan.FromSeconds(keepAliveIntervalSec);
            ReceiveBufferSize = receiveBufferSize;
            SendBufferSize = sendBufferSize;
        }
    }
}
