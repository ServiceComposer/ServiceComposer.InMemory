using System;
using System.Threading.Tasks;

namespace ServiceComposer.InMemory
{
    public interface ICompositionErrorsHandler
    {
        Task OnRequestError(ICompositionContext context, Exception ex);
    }
}