using PHS.Networking.Server.Services;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebsocketsSimple.TestApps.Server
{
    public class MockUserService : IUserService<Guid>
    {
        public Task<bool> IsValidTokenAsync(byte[] token, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Encoding.UTF8.GetString(token) == "testToken");
        }

        public Task<Guid> GetIdAsync(byte[] token, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Guid.NewGuid());
        }
    }
}
