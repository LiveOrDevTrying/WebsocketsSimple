using System;
using System.Collections.Generic;
using System.Text;

namespace PHS.WS.Core.Server.Middleware.Models
{
    public struct WebsocketManagerParams
    {
        public string InvalidTokenString { get; set; }
        public string UnauthorizedString { get; set; }
        public string AuthorizedString { get; set; }
    }
}
