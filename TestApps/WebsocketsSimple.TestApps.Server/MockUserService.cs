using PHS.Networking.Server.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace WebsocketsSimple.TestApps.Server
{
    public class MockUserService : IUserService<Guid>
    {
        public Task<bool> TryGetIdAsync(string token, out Guid id, CancellationToken cancellationToken = default)
        {
            id = Guid.NewGuid();
            return Task.FromResult(token == "testToken");
        }

        public void Dispose()
        {
        }
    }
}
