using __Name__.Domain.Common;

namespace __Name__.Application.Abstractions.Messaging;

public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();

/// <summary>
/// Her isteğin işleyiciden önce ve sonra geçtiği adım (loglama, doğrulama, yetki, önbellek…).
/// DependencyInjection.cs içinde ilk kaydedilen davranış en dışta çalışır.
/// </summary>
public interface IPipelineBehavior<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result, IFailureFactory<TResponse>
{
    Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
}
