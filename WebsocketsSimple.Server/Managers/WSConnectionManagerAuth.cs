﻿using PHS.Networking.Server.Managers;
using WebsocketsSimple.Server.Models;

namespace WebsocketsSimple.Server.Managers
{
    public class WSConnectionManagerAuth<T> : ConnectionManagerAuth<IdentityWSServer<T>, T>
    {
    }
}
