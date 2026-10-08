//#if LocalAuth
using __Name__.Application.Abstractions.Authentication;
using __Name__.Application.Abstractions.Messaging;
using __Name__.Domain.Common;
using FluentValidation;

namespace __Name__.Application.Account;

/// <summary>Doğrulayıcı uygulamadaki kodu doğrulayıp iki adımlı doğrulamayı açar; kurtarma kodlarını döndürür.</summary>
public sealed record EnableTwoFactorCommand(string Code) : ICommand<RecoveryCodesResponse>
{
    public override string ToString() => nameof(EnableTwoFactorCommand);
}

internal sealed class EnableTwoFactorCommandValidator : AbstractValidator<EnableTwoFactorCommand>
{
    public EnableTwoFactorCommandValidator()
    {
        RuleFor(command => command.Code).NotEmpty().MaximumLength(16);
    }
}

internal sealed class EnableTwoFactorCommandHandler(IIdentityService identityService, IUserContext userContext)
    : ICommandHandler<EnableTwoFactorCommand, RecoveryCodesResponse>
{
    public Task<Result<RecoveryCodesResponse>> Handle(EnableTwoFactorCommand command, CancellationToken cancellationToken) =>
        userContext.UserId is { } userId
            ? identityService.EnableTwoFactorAsync(userId, command.Code, cancellationToken)
            : Task.FromResult<Result<RecoveryCodesResponse>>(UserContextErrors.NotAuthenticated);
}
//#endif
