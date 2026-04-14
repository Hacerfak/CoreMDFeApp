using System;
using System.Threading;
using System.Threading.Tasks;

namespace CoreMDFe.Application.Mediator
{
    // 1. As interfaces que substituem o MediatR
    public interface IRequest<out TResponse> { }

    // Interface para comandos que não retornam nada (Void/Unit)
    public interface IRequest : IRequest<Unit> { }

    public interface IRequestHandler<in TRequest, TResponse> where TRequest : IRequest<TResponse>
    {
        Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
    }

    public interface IMediator
    {
        Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
    }

    // Struct auxiliar para substituir o MediatR.Unit
    public struct Unit
    {
        public static readonly Unit Value = new Unit();
    }

    // 2. A implementação nativa do Mediator
    public class NativeMediator : IMediator
    {
        private readonly IServiceProvider _serviceProvider;

        public NativeMediator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var requestType = request.GetType();

            // Descobre qual é o Handler correto para este comando/query
            var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, typeof(TResponse));

            // Pede ao contêiner de injeção de dependência nativo do .NET para criar o Handler
            var handler = _serviceProvider.GetService(handlerType);

            if (handler == null)
                throw new InvalidOperationException($"Nenhum Handler foi registado para o comando/query: {requestType.Name}");

            // Executa o método Handle do Handler encontrado
            var method = handlerType.GetMethod("Handle");
            return (Task<TResponse>)method!.Invoke(handler, new object[] { request, cancellationToken })!;
        }
    }
}