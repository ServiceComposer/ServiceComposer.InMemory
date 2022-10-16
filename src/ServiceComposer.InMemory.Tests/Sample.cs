using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ServiceComposer.InMemory.Tests
{
    public class Sample
    {
        class Handler : ICompositionRequestsHandler
        {
            public Task Handle(ICompositionContext context)
            {
                // how to access the view model? Using the same GetComposedResponseModel
                // as in Asp.NetCore but on the contenxt? e.g.:
                //var vm = context.GetComposedResponseModel();

                // should te default view model be the usual dynamic object?

                return Task.CompletedTask;
            }
        }

        [Fact]
        public async Task Should_run()
        {
            // In a console app the following would be the minimum
            // required configuration. Given that there is no "run"
            // phase, there is no place to initialize the composition
            // engine.
            // One option is to do that when adding stuff to the ServiceCollection
            // Another option woul dbe to require usage of the Host and register a 
            // HostedService that does te heavy lifting when the application host
            // starts.

            var services = new ServiceCollection();
            services.AddInMemoryViewModelComposition(options => 
            {
                options.AssemblyScanner.Disable();
                options.RegisterCompositionHandler<Handler>();
            });

            var serviceProvider = services.BuildServiceProvider();
            var compositionEngine = serviceProvider.GetRequiredService<ICompositionEngine>();

            var composedViewModel = await compositionEngine.HandleComposableRequest("a/request");
        }
    }
}