using System.Threading.Tasks;

namespace ServiceComposer.InMemory
{
    public interface ICompositionContext
    {
        string RequestId { get; }
        Task RaiseEvent(object @event);
    }
}