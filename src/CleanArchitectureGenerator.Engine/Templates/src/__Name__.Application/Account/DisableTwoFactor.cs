//#if LocalAuth
using __Name__.Application.Abstractions.Authentication;
using __Name__.Application.Abstractions.Messaging;
using __Name__.Application.Authentication;
using __Name__.Domain.Common;
using FluentValidation;

namespace __Name__.Application.Account;

/// <summary>İki adımlı doğrulamayı kapatır. Ele geçirilmiş bir oturumla kapatılamaması için parola istenir.</summary>
public sealed record DisableTwoFactorCommand(string Password) : ICommand
{
    public override string ToString() => nameof(DisableTwoFactorCommand);
}

internal sealed class DisableTwoFactorCommandValidator : AbstractValidator<DisableTwoFactorCommand>
{
    public DisableTwoFactorCommandValidator()
    {
        RuleFor(command => command.Password).ValidCurrentPassword();
    }
}

internal sealed class DisableTwoFactorCommandHandler(IIdentityService identityService, IUserContext userContext)
    : ICommandHandler<DisableTwoFactorCommand>
{
    public Task<Result> Handle(DisableTwoFactorCommand command, CancellationToken cancellationToken) =>
        userContext.UserId is { } userId
            ? identityService.DisableTwoFactorAsync(userId, command.Password, cancellationToken)
            : Task.FromResult<Result>(UserContextErrors.NotAuthenticated);
}
//#endif
