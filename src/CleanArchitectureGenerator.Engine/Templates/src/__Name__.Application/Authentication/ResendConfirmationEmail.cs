//#if LocalAuth
using __Name__.Application.Abstractions.Authentication;
using __Name__.Application.Abstractions.Messaging;
using __Name__.Domain.Common;
using FluentValidation;

namespace __Name__.Application.Authentication;

/// <summary>Doğrulanmamış bir hesaba yeni doğrulama bağlantısı gönderir. Sonuç her durumda aynıdır.</summary>
public sealed record ResendConfirmationEmailCommand(string Email) : ICommand
{
    public override string ToString() => nameof(ResendConfirmationEmailCommand);
}

internal sealed class ResendConfirmationEmailCommandValidator : AbstractValidator<ResendConfirmationEmailCommand>
{
    public ResendConfirmationEmailCommandValidator()
    {
        RuleFor(command => command.Email).ValidEmail();
    }
}

internal sealed class ResendConfirmationEmailCommandHandler(IIdentityService identityService)
    : ICommandHandler<ResendConfirmationEmailCommand>
{
    public async Task<Result> Handle(ResendConfirmationEmailCommand command, CancellationToken cancellationToken)
    {
        await identityService.ResendConfirmationEmailAsync(command.Email, cancellationToken);
        return Result.Success();
    }
}
//#endif
