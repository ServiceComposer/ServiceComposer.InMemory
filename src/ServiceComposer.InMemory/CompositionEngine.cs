using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ServiceComposer.InMemory
{
    public interface ICompositionEngine
    {
        Task<object> HandleComposableRequest(string routeKey);
    }

    class CompositionEngine : ICompositionEngine
    {
        CompositionMetadataRegistry compositionMetadataRegistry;
        IServiceProvider serviceProvider;

        public CompositionEngine(CompositionMetadataRegistry compositionMetadataRegistry, IServiceProvider serviceProvider)
        {
            this.compositionMetadataRegistry = compositionMetadataRegistry;
            this.serviceProvider = serviceProvider;
        }

        public async Task<object> HandleComposableRequest(string routeKey)
        {
            var requestId = Guid.NewGuid().ToString();
            var compositionContext = new CompositionContext(requestId);

            object viewModel;
            var factoryType = componentsTypes.SingleOrDefault(t => typeof(IEndpointScopedViewModelFactory).IsAssignableFrom(t)) ?? typeof(IViewModelFactory);
            var viewModelFactory = (IViewModelFactory)serviceProvider.GetService(factoryType);
            if (viewModelFactory != null)
            {
                viewModel = viewModelFactory.CreateViewModel(compositionContext);
            }
            else
            {
                var logger = serviceProvider.GetRequiredService<ILogger<DynamicViewModel>>();
                viewModel = new DynamicViewModel(logger, compositionContext);
            }

            try
            {
                var handlers = componentsTypes.Select(type => serviceProvider.GetRequiredService(type)).ToArray();
                //TODO: if handlers == none we could shortcut to 404 here

                foreach (var subscriber in handlers.OfType<ICompositionEventsSubscriber>())
                {
                    subscriber.Subscribe(compositionContext);
                }

                //TODO: if handlers == none we could shortcut again to 404 here
                var pending = handlers.OfType<ICompositionRequestsHandler>()
                    .Select(handler => handler.Handle(compositionContext))
                    .ToList();

                if (pending.Count == 0)
                {
                    return null;
                }
                else
                {
                    try
                    {
                        await Task.WhenAll(pending);
                    }
                    catch (Exception ex)
                    {
                        //TODO: refactor to Task.WhenAll
                        var errorHandlers = handlers.OfType<ICompositionErrorsHandler>();
                        foreach (var handler in errorHandlers)
                        {
                            await handler.OnRequestError(compositionContext, ex);
                        }

                        throw;
                    }
                }

                return viewModel;
            }
            finally
            {
                compositionContext.CleanupSubscribers();
            }
        }
    }
}