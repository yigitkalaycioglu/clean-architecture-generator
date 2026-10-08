using __Name__.Domain.Common;

namespace __Name__.Application.Abstractions.Messaging;

/// <summary>Bir isteği işleyen sınıf. Her istek türünün tam olarak bir işleyicisi olur (ArchitectureTests denetler).</summary>
public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result, IFailureFactory<TResponse>
{
    Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}

public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand, Result>
    where TCommand : ICommand;

public interface ICommandHandler<in TCommand, TValue> : IRequestHandler<TCommand, Result<TValue>>
    where TCommand : ICommand<TValue>;

public interface IQueryHandler<in TQuery, TValue> : IRequestHandler<TQuery, Result<TValue>>
    where TQuery : IQuery<TValue>;
