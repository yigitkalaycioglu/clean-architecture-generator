//#if LocalAuth
using __Name__.Application.Abstractions.Authentication;
using __Name__.Application.Abstractions.Messaging;
using __Name__.Domain.Common;
using FluentValidation;

namespace __Name__.Application.Authentication;

/// <summary>
/// Yeni hesap açar ve doğrulama e-postası gönderir. Adres zaten kayıtlıysa da aynı yanıt döner (hesap sahibine
/// bilgilendirme e-postası gider); böylece kayıt formu "bu e-posta kayıtlı mı?" sorusuna cevap vermez.
/// </summary>
public sealed record RegisterCommand(string Email, string Password) : ICommand
{
    public override string ToString() => nameof(RegisterCommand);
}

internal sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(command => command.Email).ValidEmail();
        RuleFor(command => command.Password).ValidNewPassword();
    }
}

internal sealed class RegisterCommandHandler(IIdentityService identityService) : ICommandHandler<RegisterCommand>
{
    public Task<Result> Handle(RegisterCommand command, CancellationToken cancellationToken) =>
        identityService.RegisterAsync(command.Email, command.Password, cancellationToken);
}
//#endif
