using System;
using System.Collections.Generic;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace WebsocketsSimple.Client.Models
{
    public interface IParamsWSClient
    {
        string Host { get; set; }
        int Port { get; set; }
        string Path { get; set; }
        KeyValuePair<string, string>[] QueryStringParameters { get; set; }
        bool IsWebsocketSecured { get; set; }

        IDictionary<string, string> RequestHeaders { get; set; }
        string[] RequestedSubProtocols { get; set; }
        X509Certificate2[] ClientCertificates { get; set; }
        SslProtocols EnabledSslProtocols { get; set; }
        TimeSpan KeepAliveInterval { get; set; }
        int ReceiveBufferSize { get; set; }
        int SendBufferSize { get; set; }
    }
}
