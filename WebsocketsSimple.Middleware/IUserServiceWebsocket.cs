using PHS.Core.Services;
using WebsocketsSimple.Middleware.Models;
using System;
using System.Threading.Tasks;

namespace WebsocketsSimple.Middleware
{
    public interface IUserServiceWebsocket : IUserService
    {
        Task<IBotWS> GetWSBotAsync(string token);
        Task<IBotWS> GetWSBotAsync(Guid id);
    }
}
