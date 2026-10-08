using __Name__.Domain.Common;

namespace __Name__.Application.Abstractions.Messaging;

/// <summary>
/// Uygulamaya gönderilen her istek bir <see cref="Result"/> döndürür: beklenen hatalar istisnayla değil,
/// türü belli bir sonuçla taşınır. İstekler <see cref="ISender"/> ile gönderilir.
/// </summary>
public interface IRequest<TResponse>
    where TResponse : Result, IFailureFactory<TResponse>;

/// <summary>Durumu değiştiren istek (yazma).</summary>
public interface ICommand : IRequest<Result>;

/// <summary>Durumu değiştiren ve bir değer döndüren istek, ör. oluşturulan kaydın kimliği.</summary>
public interface ICommand<TValue> : IRequest<Result<TValue>>;

/// <summary>Durumu değiştirmeyen, yalnızca veri okuyan istek.</summary>
public interface IQuery<TValue> : IRequest<Result<TValue>>;
