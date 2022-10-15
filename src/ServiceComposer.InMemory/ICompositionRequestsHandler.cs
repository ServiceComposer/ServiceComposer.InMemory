using System.Threading.Tasks;

namespace ServiceComposer.InMemory
{
    public interface ICompositionRequestsHandler
    {
        Task Handle(ICompositionContext context);
    }
}