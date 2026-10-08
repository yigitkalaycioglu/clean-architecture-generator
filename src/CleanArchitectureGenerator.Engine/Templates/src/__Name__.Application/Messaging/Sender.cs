using System.Collections.Concurrent;
using __Name__.Application.Abstractions.Messaging;
using __Name__.Domain.Common;
using Microsoft.Extensions.DependencyInjection;

namespace __Name__.Application.Messaging;

internal sealed class Sender(IServiceProvider serviceProvider) : ISender
{
    // İstek türü başına bir kez, yansıma (reflection) ile oluşturulan tür güvenli işlem hattı.
    private static readonly ConcurrentDictionary<Type, object> Pipelines = new();

    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        where TResponse : Result, IFailureFactory<TResponse>
    {
        ArgumentNullException.ThrowIfNull(request);

        var pipeline = (RequestPipeline<TResponse>)Pipelines.GetOrAdd(
            request.GetType(),
            static (requestType, responseType) =>
                Activator.CreateInstance(typeof(RequestPipeline<,>).MakeGenericType(requestType, responseType))!,
            typeof(TResponse));

        return pipeline.Handle(request, serviceProvider, cancellationToken);
    }
}

internal abstract class RequestPipeline<TResponse>
    where TResponse : Result, IFailureFactory<TResponse>
{
    public abstract Task<TResponse> Handle(IRequest<TResponse> request, IServiceProvider serviceProvider, CancellationToken cancellationToken);
}

internal sealed class RequestPipeline<TRequest, TResponse> : RequestPipeline<TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result, IFailureFactory<TResponse>
{
    public override Task<TResponse> Handle(IRequest<TResponse> request, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var typedRequest = (TRequest)request;
        var handler = serviceProvider.GetRequiredService<IRequestHandler<TRequest, TResponse>>();

        RequestHandlerDelegate<TResponse> next = () => handler.Handle(typedRequest, cancellationToken);

        // Davranışlar sondan başa sarılır; böylece ilk kaydedilen davranış en dışta çalışır.
        foreach (var behavior in serviceProvider.GetServices<IPipelineBehavior<TRequest, TResponse>>().Reverse())
        {
            var inner = next;
            next = () => behavior.Handle(typedRequest, inner, cancellationToken);
        }

        return next();
    }
}
