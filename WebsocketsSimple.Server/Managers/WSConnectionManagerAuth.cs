﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using WebsocketsSimple.Server.Models;

namespace WebsocketsSimple.Server.Managers
{
    public class WSConnectionManagerAuth<T> : WSConnectionManager<IdentityWSServer<T>>
    {
        protected ConcurrentDictionary<T, WSConnectionManager<IdentityWSServer<T>>> _users =
            new ConcurrentDictionary<T, WSConnectionManager<IdentityWSServer<T>>>();

        public virtual bool Add(IdentityWSServer<T> identity)
        {
            Add(identity.ConnectionId, identity);

            if (!_users.TryGetValue(identity.UserId, out var userOriginal))
            {
                userOriginal = new WSConnectionManager<IdentityWSServer<T>>();
                if (!_users.TryAdd(identity.UserId, userOriginal))
                {
                    return false;
                }
            }

            var userNew = new WSConnectionManager<IdentityWSServer<T>>(userOriginal.GetAll());
            userNew.Add(identity.ConnectionId, identity);
            return _users.TryUpdate(identity.UserId, userNew, userOriginal);
        }
        public override bool Remove(string id)
        {
            _connections.TryRemove(id, out var _);

            try
            {
                foreach (var user in _users)
                {
                    if (user.Value.Remove(id))
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
            }

            return false;
        }
        public IEnumerable<IdentityWSServer<T>> GetAll(T id)
        {
            if (_users.TryGetValue(id, out var user))
            {
                return user.GetAll();
            }

            return null;
        }
    }
}
