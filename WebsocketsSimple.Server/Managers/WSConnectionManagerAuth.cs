using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using WebsocketsSimple.Server.Models;

namespace WebsocketsSimple.Server.Managers
{
    public class WSConnectionManagerAuth<T> : WSConnectionManagerAuthBase<IdentityWSServer<T>, T>
    {
    }
}
