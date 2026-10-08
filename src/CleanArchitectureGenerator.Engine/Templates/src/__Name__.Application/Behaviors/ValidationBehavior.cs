using System.Text.Json;
using __Name__.Application.Abstractions.Messaging;
using __Name__.Domain.Common;
using FluentValidation;
using FluentValidation.Results;

namespace __Name__.Application.Behaviors;

/// <summary>
/// İsteği, işleyiciye ulaşmadan önce o isteğe ait tüm FluentValidation doğrulayıcılarıyla denetler.
/// Geçersiz istek işleyiciye hiç ulaşmaz; alan adına göre gruplanmış hatalarla başarısız sonuç döner.
/// </summary>
internal sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result, IFailureFactory<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        var failures = new List<ValidationFailure>();
        foreach (var validator in validators)
        {
            var result = await validator.ValidateAsync(new ValidationContext<TRequest>(request), cancellationToken);
            failures.AddRange(result.Errors);
        }

        if (failures.Count == 0)
        {
            return await next();
        }

        // Alan adları JSON gövdesindeki gibi camelCase döner (ör. "title").
        var details = failures
            .GroupBy(failure => JsonNamingPolicy.CamelCase.ConvertName(failure.PropertyName), StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Select(failure => failure.ErrorMessage).Distinct(StringComparer.Ordinal).ToArray(),
                StringComparer.Ordinal);

        return TResponse.CreateFailure(Error.Validation(details));
    }
}
