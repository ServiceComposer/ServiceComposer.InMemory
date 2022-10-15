namespace ServiceComposer.InMemory
{
    public interface ICompositionEventsSubscriber
    {
        void Subscribe(ICompositionEventsPublisher publisher);
    }
}