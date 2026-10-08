//#if LocalAuth
using __Name__.Application.Abstractions.Authentication;
using __Name__.Application.Abstractions.Messaging;
using __Name__.Domain.Common;

namespace __Name__.Application.Authentication;

/// <summary>Oturumu kapatır: yenileme token'ı ve aynı oturumdan türeyen tüm token'lar iptal edilir.</summary>
public sealed record LogoutCommand(string? RefreshToken) : ICommand
{
    public override string ToString() => nameof(LogoutCommand);
}

internal sealed class LogoutCommandHandler(IIdentityService identityService) : ICommandHandler<LogoutCommand>
{
    public async Task<Result> Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(command.RefreshToken) && command.RefreshToken.Length <= 256)
        {
            await identityService.LogoutAsync(command.RefreshToken, cancellationToken);
        }

        return Result.Success();
    }
}
//#endif
