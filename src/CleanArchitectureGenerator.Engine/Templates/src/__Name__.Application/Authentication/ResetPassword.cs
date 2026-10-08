//#if LocalAuth
using __Name__.Application.Abstractions.Authentication;
using __Name__.Application.Abstractions.Messaging;
using __Name__.Domain.Common;
using FluentValidation;

namespace __Name__.Application.Authentication;

/// <summary>E-postadaki kodla yeni parola belirler. Başarılı olursa kullanıcının tüm açık oturumları kapatılır.</summary>
public sealed record ResetPasswordCommand(string Email, string Code, string NewPassword) : ICommand
{
    public override string ToString() => nameof(ResetPasswordCommand);
}

internal sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(command => command.Email).ValidEmail();
        RuleFor(command => command.Code).ValidCode();
        RuleFor(command => command.NewPassword).ValidNewPassword();
    }
}

internal sealed class ResetPasswordCommandHandler(IIdentityService identityService) : ICommandHandler<ResetPasswordCommand>
{
    public Task<Result> Handle(ResetPasswordCommand command, CancellationToken cancellationToken) =>
        identityService.ResetPasswordAsync(command.Email, command.Code, command.NewPassword, cancellationToken);
}
//#endif
