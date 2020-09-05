using Digify.DependecyInjection;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Digify.Events
{
    public static class ServiceCollectionExtension
    {
        public static IServiceCollection AddEvents(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<IEventBus, DefaultEventBus>();
            serviceCollection.AddSingleton<IEventBusState, EventBusState>();
            serviceCollection.AddAutoDependency().AddServices();


            return serviceCollection;
        }
        private static IServiceCollection AddServices(this IServiceCollection serviceCollection)
        {
            var eventHandlers = serviceCollection
               .Select(x => x.ImplementationType)
               .Distinct()
               .Where(t => t != null && typeof(IEventHandler).IsAssignableFrom(t) && t.GetTypeInfo().IsClass)
               .ToArray();

            foreach (var handlerClass in eventHandlers)
            {
                serviceCollection.AddScoped(handlerClass);

                // Register dynamic proxies to intercept direct calls if an IEventHandler is resolved, dispatching the call to
                // the event bus.

                foreach (var i in handlerClass.GetInterfaces().Where(t => typeof(IEventHandler).IsAssignableFrom(t)))
                {
                    serviceCollection.AddScoped(i, serviceProvider =>
                    {
                        var proxy = DefaultEventBus.CreateProxy(i);
                        proxy.EventBus = serviceProvider.GetService<IEventBus>();
                        return proxy;
                    });
                }
            }

            var shellServiceProvider = serviceCollection.BuildServiceProvider();
            var eventBusState = shellServiceProvider.GetService<IEventBusState>();

            // Register any IEventHandler method in the event bus
            foreach (var handlerClass in eventHandlers)
            {
                foreach (var handlerInterface in handlerClass.GetInterfaces().Where(x => typeof(IEventHandler).IsAssignableFrom(x) && typeof(IEventHandler) != x))
                {
                    foreach (var interfaceMethod in handlerInterface.GetMethods())
                    {
                        Func<IServiceProvider, IDictionary<string, object>, Task> d = (sp, parameters) => DefaultEventBus.Invoke(sp, parameters, interfaceMethod, handlerClass);
                        eventBusState.Add(handlerInterface.Name + "." + interfaceMethod.Name, d);
                    }
                }
            }
            var serviceDescriptor = serviceCollection.FirstOrDefault(descriptor => descriptor.ServiceType == typeof(IEventBusState));
            if (serviceDescriptor != null)
            {
                serviceCollection.Remove(serviceDescriptor);
            }
            serviceCollection.AddSingleton(eventBusState);
            return serviceCollection;
        }
    }
}
