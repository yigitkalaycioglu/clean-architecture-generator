//#if LocalAuth
using __Name__.Application.Abstractions.Authentication;
using __Name__.Application.Abstractions.Messaging;
using __Name__.Domain.Common;
using FluentValidation;

namespace __Name__.Application.Authentication;

/// <summary>
/// Doğrulanmış bir hesaba parola sıfırlama bağlantısı gönderir. Adres kayıtlı olsun ya da olmasın sonuç aynıdır.
/// </summary>
public sealed record ForgotPasswordCommand(string Email) : ICommand
{
    public override string ToString() => nameof(ForgotPasswordCommand);
}

internal sealed class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        RuleFor(command => command.Email).ValidEmail();
    }
}

internal sealed class ForgotPasswordCommandHandler(IIdentityService identityService) : ICommandHandler<ForgotPasswordCommand>
{
    public async Task<Result> Handle(ForgotPasswordCommand command, CancellationToken cancellationToken)
    {
        await identityService.ForgotPasswordAsync(command.Email, cancellationToken);
        return Result.Success();
    }
}
//#endif
