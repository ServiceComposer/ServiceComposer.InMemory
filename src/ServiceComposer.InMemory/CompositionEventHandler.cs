using System.Threading.Tasks;

namespace ServiceComposer.InMemory
{
    public delegate Task CompositionEventHandler<in TEvent>(TEvent @event, ICompositionContext context);
}