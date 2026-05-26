using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Reflection;

namespace CoreMDFe.Application.Mediator
{
    public static class MediatorExtensions
    {
        public static IServiceCollection AddNativeMediator(this IServiceCollection services, Assembly assembly)
        {
            // Regista o serviço central do Mediator
            services.AddSingleton<IMediator, NativeMediator>();

            // Procura todas as classes que implementam pelo menos um IRequestHandler<,>
            var handlerTypes = assembly.GetTypes()
                .Where(t => t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)))
                .ToList();

            // Regista CADA UMA das interfaces que a classe implementa
            foreach (var type in handlerTypes)
            {
                var interfacesImplementadas = type.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>));

                foreach (var interfaceType in interfacesImplementadas)
                {
                    services.AddTransient(interfaceType, type);
                }
            }

            return services;
        }
    }
}