using PHS.Core.Services;
using PHS.WS.Core.Server.Middleware.Models;
using System;
using System.Threading.Tasks;

namespace PHS.WS.Core.Server.Middleware
{
    public interface IUserServiceWebsocket : IUserService
    {
        Task<IBotWS> GetWSBotAsync(string token);
        Task<IBotWS> GetWSBotAsync(Guid id);
    }
}
