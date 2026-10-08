//#if LocalAuth
using __Name__.Application.Abstractions.Authentication;
using __Name__.Application.Abstractions.Messaging;
using __Name__.Application.Authentication;
using __Name__.Domain.Common;
using FluentValidation;

namespace __Name__.Application.Account;

/// <summary>Yeni kurtarma kodları üretir; eski kodların hepsi geçersiz olur. Parola istenir.</summary>
public sealed record RegenerateRecoveryCodesCommand(string Password) : ICommand<RecoveryCodesResponse>
{
    public override string ToString() => nameof(RegenerateRecoveryCodesCommand);
}

internal sealed class RegenerateRecoveryCodesCommandValidator : AbstractValidator<RegenerateRecoveryCodesCommand>
{
    public RegenerateRecoveryCodesCommandValidator()
    {
        RuleFor(command => command.Password).ValidCurrentPassword();
    }
}

internal sealed class RegenerateRecoveryCodesCommandHandler(IIdentityService identityService, IUserContext userContext)
    : ICommandHandler<RegenerateRecoveryCodesCommand, RecoveryCodesResponse>
{
    public Task<Result<RecoveryCodesResponse>> Handle(RegenerateRecoveryCodesCommand command, CancellationToken cancellationToken) =>
        userContext.UserId is { } userId
            ? identityService.RegenerateRecoveryCodesAsync(userId, command.Password, cancellationToken)
            : Task.FromResult<Result<RecoveryCodesResponse>>(UserContextErrors.NotAuthenticated);
}
//#endif
