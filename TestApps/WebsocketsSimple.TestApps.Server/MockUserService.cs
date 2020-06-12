using PHS.Networking.Server.Services;
using System;
using System.Threading.Tasks;

namespace WebsocketsSimple.TestApps.Server
{
    public class MockUserService : IUserService<Guid>
    { 
        public virtual Task<Guid> GetIdAsync(string token)
        {
            return Task.FromResult(Guid.NewGuid());
        }

        public Task<bool> IsValidTokenAsync(string token)
        {
            return Task.FromResult(true);
        }

        public virtual void Dispose()
        {
        }

    }
}
