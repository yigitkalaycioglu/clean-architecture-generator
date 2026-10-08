//#if LocalAuth
using __Name__.Application.Abstractions.Authentication;
using __Name__.Application.Abstractions.Messaging;
using __Name__.Application.Authentication;
using __Name__.Domain.Common;
using FluentValidation;

namespace __Name__.Application.Account;

/// <summary>Parolayı değiştirir. Başarılı olursa kullanıcının tüm açık oturumları kapatılır.</summary>
public sealed record ChangePasswordCommand(string CurrentPassword, string NewPassword) : ICommand
{
    public override string ToString() => nameof(ChangePasswordCommand);
}

internal sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(command => command.CurrentPassword).ValidCurrentPassword();
        RuleFor(command => command.NewPassword).ValidNewPassword().NotEqual(command => command.CurrentPassword);
    }
}

internal sealed class ChangePasswordCommandHandler(IIdentityService identityService, IUserContext userContext)
    : ICommandHandler<ChangePasswordCommand>
{
    public Task<Result> Handle(ChangePasswordCommand command, CancellationToken cancellationToken) =>
        userContext.UserId is { } userId
            ? identityService.ChangePasswordAsync(userId, command.CurrentPassword, command.NewPassword, cancellationToken)
            : Task.FromResult<Result>(UserContextErrors.NotAuthenticated);
}
//#endif
