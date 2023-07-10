using PHS.Networking.Models;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Net.WebSockets;

namespace WebsocketsSimple.Core.Models
{
    public class ConnectionWS : IConnection
    {
        public WebSocket Websocket { get; set; }
        public TcpClient TcpClient { get; set; }
        public MemoryStream MemoryStream { get; set; }
        public string ConnectionId { get; set; }
        public bool Disposed { get; set; }
        public SslStream SslStream { get; set; }
        public byte[] ReadBuffer { get; set; }

        public ConnectionWS()
        {
            MemoryStream = new MemoryStream();
        }

        public virtual void Dispose()
        {
            Disposed = true;

            try
            {
                SslStream?.Close();
            }
            catch { }

            try
            {
                SslStream?.Dispose();
            }
            catch { }

            try
            {
                Websocket?.Dispose();
            }
            catch { }

            try
            {
                TcpClient?.GetStream()?.Close();
            }
            catch { }

            try
            {
                TcpClient?.Dispose();
            }
            catch { }

            try
            {
                MemoryStream?.Close();
            } catch { }

            try {
                MemoryStream?.Dispose();
            }
            catch { }
        }
    }
}
