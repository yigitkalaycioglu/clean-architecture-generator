using __Name__.Domain.Common;

namespace __Name__.Application.Abstractions.Messaging;

/// <summary>
/// İsteği işleyicisine, kayıtlı davranışlardan geçirerek iletir. Harici bir kütüphaneye (MediatR vb.)
/// bağımlı olmayan, yalnızca bu çözümün kodundan oluşan küçük bir uygulamadır.
/// </summary>
public interface ISender
{
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        where TResponse : Result, IFailureFactory<TResponse>;
}
