//#if LocalAuth
using __Name__.Application.Abstractions.Authentication;
using __Name__.Application.Abstractions.Messaging;
using __Name__.Domain.Common;
using FluentValidation;

namespace __Name__.Application.Authentication;

/// <summary>
/// Yenileme token'ıyla yeni bir erişim token'ı alır. Yenileme token'ı her kullanımda değişir (rotation);
/// kullanılmış bir token tekrar gelirse çalınmış sayılır ve o oturum ailesinin tüm token'ları iptal edilir.
/// </summary>
public sealed record RefreshSessionCommand(string RefreshToken) : ICommand<AuthTokens>
{
    public override string ToString() => nameof(RefreshSessionCommand);
}

internal sealed class RefreshSessionCommandValidator : AbstractValidator<RefreshSessionCommand>
{
    public RefreshSessionCommandValidator()
    {
        RuleFor(command => command.RefreshToken).NotEmpty().MaximumLength(256);
    }
}

internal sealed class RefreshSessionCommandHandler(IIdentityService identityService) : ICommandHandler<RefreshSessionCommand, AuthTokens>
{
    public Task<Result<AuthTokens>> Handle(RefreshSessionCommand command, CancellationToken cancellationToken) =>
        identityService.RefreshAsync(command.RefreshToken, cancellationToken);
}
//#endif
