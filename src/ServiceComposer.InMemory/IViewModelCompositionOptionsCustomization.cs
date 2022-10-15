namespace ServiceComposer.InMemory
{
    public interface IViewModelCompositionOptionsCustomization
    {
        void Customize(InMemoryViewModelCompositionOptions options);
    }
}