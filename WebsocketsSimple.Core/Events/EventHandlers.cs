using PHS.Core.Events.Args;
using System.Threading.Tasks;

namespace WebsocketsSimple.Core.Events
{
    public delegate Task NetworkingEventHandler<T>(object sender, T e) where T : BaseArgs;
}
