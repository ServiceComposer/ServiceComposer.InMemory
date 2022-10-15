namespace ServiceComposer.InMemory
{
    public interface ICompositionEventsPublisher
    {
        void Subscribe<TEvent>(CompositionEventHandler<TEvent> handler);
    }
}