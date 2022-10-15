using Microsoft.AspNetCore.Http;

namespace ServiceComposer.InMemory
{
    public interface IViewModelFactory
    {
        object CreateViewModel(ICompositionContext compositionContext);
    }
}