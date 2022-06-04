using System.Collections.Concurrent;
using System.Collections.Generic;
using WebsocketsSimple.Server.Models;

namespace WebsocketsSimple.Server.Managers
{
    public class WSConnectionManagerAuthBase<Z, A> : WSConnectionManagerBase<Z> where Z : IdentityWSServer<A>
    {
        protected ConcurrentDictionary<A, WSConnectionManagerBase<Z>> _users =
            new ConcurrentDictionary<A, WSConnectionManagerBase<Z>>();

        public virtual bool Add(Z identity)
        {
            Add(identity.ConnectionId, identity);

            if (!_users.TryGetValue(identity.UserId, out var userOriginal))
            {
                userOriginal = new WSConnectionManagerBase<Z>();
                if (!_users.TryAdd(identity.UserId, userOriginal))
                {
                    return false;
                }
            }

            var userNew = new WSConnectionManagerBase<Z>(userOriginal.GetAllDictionary());
            userNew.Add(identity.ConnectionId, identity);
            return _users.TryUpdate(identity.UserId, userNew, userOriginal);
        }
        public override bool Remove(string id)
        {
            _connections.TryRemove(id, out var _);

            try
            {
                A userToRemove = default;
                bool removeUser = false;
                foreach (var user in _users)
                {
                    if (user.Value.Remove(id))
                    {
                        if (user.Value.Count() == 0)
                        {
                            userToRemove = user.Key;
                            removeUser = true;
                            break;
                        }

                        return true;
                    }
                }

                if (removeUser)
                {
                    _users.TryRemove(userToRemove, out var _);
                    return true;
                }
            }
            catch
            { }

            return false;
        }

        public IEnumerable<Z> GetAll(A id)
        {
            if (_users.TryGetValue(id, out var user))
            {
                return user.GetAll();
            }

            return null;
        }
    }
}
