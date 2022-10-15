using Microsoft.Extensions.DependencyInjection;
using System;
using Microsoft.Extensions.Configuration;

namespace ServiceComposer.InMemory
{
    public static class ServiceCollectionExtensions
    {
        public static void AddInMemoryViewModelComposition(this IServiceCollection services, IConfiguration configuration = null)
        {
            AddInMemoryViewModelComposition(services, null, configuration);
        }

        public static void AddInMemoryViewModelComposition(this IServiceCollection services, Action<InMemoryViewModelCompositionOptions> config, IConfiguration configuration = null)
        {
            var options = new InMemoryViewModelCompositionOptions(services, configuration);
            config?.Invoke(options);

            options.InitializeServiceCollection();
        }
    }
}
