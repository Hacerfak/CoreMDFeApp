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

            // Procura todas as classes que implementam IRequestHandler<,>
            var handlerTypes = assembly.GetTypes()
                .Where(t => t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)))
                .ToList();

            // Regista cada Handler encontrado no Inversor de Dependência nativo
            foreach (var type in handlerTypes)
            {
                var interfaceType = type.GetInterfaces().First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>));
                services.AddTransient(interfaceType, type);
            }

            return services;
        }
    }
}