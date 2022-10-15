using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace ServiceComposer.InMemory
{
    public class InMemoryViewModelCompositionOptions
    {
        readonly IConfiguration _configuration;
        readonly CompositionMetadataRegistry _compositionMetadataRegistry = new CompositionMetadataRegistry();
        
        internal InMemoryViewModelCompositionOptions(IServiceCollection services, IConfiguration configuration = null)
        {
            _configuration = configuration;
            Services = services;
            AssemblyScanner = new AssemblyScanner();

            Services.AddSingleton(this);
            Services.AddSingleton(_compositionMetadataRegistry);
            Services.AddSingleton<ICompositionEngine, CompositionEngine>();
        }

        internal Func<Type, bool> TypesFilter { get; set; } = _ => true;

        readonly List<(Func<Type, bool>, Action<IEnumerable<Type>>)> typesRegistrationHandlers = new();
        readonly Dictionary<Type, Action<Type, IServiceCollection>> configurationHandlers = new();

        public void AddServicesConfigurationHandler(Type serviceType, Action<Type, IServiceCollection> configurationHandler)
        {
            if (configurationHandlers.ContainsKey(serviceType))
            {
                throw new NotSupportedException($"There is already a Services configuration handler for the {serviceType}.");
            }

            configurationHandlers.Add(serviceType, configurationHandler);
        }

        public void AddTypesRegistrationHandler(Func<Type, bool> typesFilter, Action<IEnumerable<Type>> registrationHandler)
        {
            typesRegistrationHandlers.Add((typesFilter, registrationHandler));
        }

        internal bool IsWriteSupportEnabled { get; private set; }

        public void EnableWriteSupport()
        {
            IsWriteSupportEnabled = true;
        }

        internal void InitializeServiceCollection()
        {
            if (AssemblyScanner.IsEnabled)
            {
                AddTypesRegistrationHandler(
                    typesFilter: type =>
                    {
                        var typeInfo = type.GetTypeInfo();
                        return !typeInfo.IsInterface
                               && !typeInfo.IsAbstract
                               && (typeof(ICompositionRequestsHandler).IsAssignableFrom(type) || typeof(ICompositionEventsSubscriber).IsAssignableFrom(type));
                    },
                    registrationHandler: types =>
                    {
                        foreach (var type in types)
                        {
                            RegisterCompositionComponents(type);
                        }
                    });

                AddTypesRegistrationHandler(
                    typesFilter: type =>
                    {
                        var typeInfo = type.GetTypeInfo();
                        return !typeInfo.IsInterface
                               && !typeInfo.IsAbstract
                               && typeof(IViewModelFactory).IsAssignableFrom(type)
                               && !typeof(IEndpointScopedViewModelFactory).IsAssignableFrom(type);
                    },
                    registrationHandler: types =>
                    {
                        foreach (var type in types)
                        {
                            RegisterGlobalViewModelFactory(type);
                        }
                    });

                AddTypesRegistrationHandler(
                    typesFilter: type =>
                    {
                        var typeInfo = type.GetTypeInfo();
                        return !typeInfo.IsInterface
                               && !typeInfo.IsAbstract
                               && typeof(IEndpointScopedViewModelFactory).IsAssignableFrom(type);
                    },
                    registrationHandler: types =>
                    {
                        foreach (var type in types)
                        {
                            Services.AddTransient(typeof(IEndpointScopedViewModelFactory), type);
                        }
                    });

                var assemblies = AssemblyScanner.Scan();
                var allTypes = assemblies
                    .SelectMany(assembly => assembly.GetTypes())
                    .Where(TypesFilter)
                    .Distinct()
                    .ToList();

                var optionsCustomizations = allTypes.Where(t => !t.IsAbstract && typeof(IViewModelCompositionOptionsCustomization).IsAssignableFrom(t));
                foreach (var optionsCustomization in optionsCustomizations)
                {
                    var oc = (IViewModelCompositionOptionsCustomization)Activator.CreateInstance(optionsCustomization);
                    Debug.Assert(oc != null, nameof(oc) + " != null");
                    oc.Customize(this);
                }

                foreach (var (typesFilter, registrationHandler) in typesRegistrationHandlers)
                {
                    var filteredTypes = allTypes.Where(typesFilter);
                    registrationHandler(filteredTypes);
                }
            }
        }

        public AssemblyScanner AssemblyScanner { get; }

        public IServiceCollection Services { get; }

        public IConfiguration Configuration
        {
            get
            {
                if (_configuration is null)
                {
                    throw new ArgumentException("No configuration instance has been set. " +
                                                "To access the application configuration call the " +
                                                "AddViewModelComposition overload te accepts an " +
                                                "IConfiguration instance.");
                }
                return _configuration;
            }
        }

        public void RegisterCompositionHandler<T>()
        {
            RegisterCompositionComponents(typeof(T));
        }

        void RegisterCompositionComponents(Type type)
        {
            if (
                !(
                    typeof(ICompositionRequestsHandler).IsAssignableFrom(type)
                    || typeof(ICompositionEventsSubscriber).IsAssignableFrom(type)
                    || typeof(IEndpointScopedViewModelFactory).IsAssignableFrom(type)
                )
            )
            {
                var message = $"Registered types must be either {nameof(ICompositionRequestsHandler)}, " +
                              $"{nameof(ICompositionEventsSubscriber)}, or {nameof(IEndpointScopedViewModelFactory)}.";

                throw new NotSupportedException(message);
            }

            _compositionMetadataRegistry.AddComponent(type);
            if (configurationHandlers.TryGetValue(type, out var handler))
            {
                handler(type, Services);
            }
            else
            {
                Services.AddTransient(type);
            }
        }

        public void RegisterEndpointScopedViewModelFactory<T>() where T : IEndpointScopedViewModelFactory
        {
            RegisterCompositionComponents(typeof(T));
        }

        public void RegisterGlobalViewModelFactory<T>() where T : IViewModelFactory
        {
            RegisterGlobalViewModelFactory(typeof(T));
        }

        void RegisterGlobalViewModelFactory(Type viewModelFactoryType)
        {
            if (viewModelFactoryType == null)
            {
                throw new ArgumentNullException(nameof(viewModelFactoryType));
            }

            if (!typeof(IViewModelFactory).IsAssignableFrom(viewModelFactoryType))
            {
                throw new ArgumentOutOfRangeException($"Type must implement {nameof(IViewModelFactory)}.");
            }

            if (typeof(IEndpointScopedViewModelFactory).IsAssignableFrom(viewModelFactoryType))
            {
                var paramName = $"To register {nameof(IEndpointScopedViewModelFactory)} use " +
                                $"the {nameof(RegisterEndpointScopedViewModelFactory)} method.";
                throw new ArgumentOutOfRangeException(paramName);
            }

            var globalFactoryRegistration = Services.SingleOrDefault(sd => sd.ServiceType == typeof(IViewModelFactory));
            if (globalFactoryRegistration != null)
            {
                var message = $"Only one global {nameof(IViewModelFactory)} is supported.";
                if (globalFactoryRegistration.ImplementationType != null)
                {
                    message += $" {globalFactoryRegistration.ImplementationType.Name} is already registered as a global view model factory.";
                }

                throw new NotSupportedException(message);
            }

            if (configurationHandlers.TryGetValue(viewModelFactoryType, out var handler))
            {
                handler(viewModelFactoryType, Services);
            }
            else
            {
                Services.AddTransient(typeof(IViewModelFactory), viewModelFactoryType);
            }
        }
    }
}