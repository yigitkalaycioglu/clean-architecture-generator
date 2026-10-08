//#if LocalAuth
using __Name__.Application.Abstractions.Authentication;
using __Name__.Application.Abstractions.Messaging;
using __Name__.Domain.Common;
using FluentValidation;

namespace __Name__.Application.Authentication;

/// <summary>
/// E-posta ve parolayla giriş. İki adımlı doğrulama açık hesaplarda doğrulayıcı uygulamadaki kod ya da bir
/// kurtarma kodu da gönderilir. Art arda hatalı denemeler hesabı geçici olarak kilitler.
/// </summary>
public sealed record LoginCommand(string Email, string Password, string? TwoFactorCode, string? RecoveryCode) : ICommand<AuthTokens>
{
    public override string ToString() => nameof(LoginCommand);
}

internal sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(command => command.Email).NotEmpty().MaximumLength(AuthenticationRules.EmailMaxLength);
        RuleFor(command => command.Password).ValidCurrentPassword();
        RuleFor(command => command.TwoFactorCode).MaximumLength(16);
        RuleFor(command => command.RecoveryCode).MaximumLength(32);
    }
}

internal sealed class LoginCommandHandler(IIdentityService identityService) : ICommandHandler<LoginCommand, AuthTokens>
{
    public Task<Result<AuthTokens>> Handle(LoginCommand command, CancellationToken cancellationToken) =>
        identityService.LoginAsync(command.Email, command.Password, command.TwoFactorCode, command.RecoveryCode, cancellationToken);
}
//#endif
