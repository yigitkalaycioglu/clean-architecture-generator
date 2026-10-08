//#if LocalAuth
using __Name__.Application.Abstractions.Authentication;
using __Name__.Application.Abstractions.Messaging;
using __Name__.Domain.Common;

namespace __Name__.Application.Account;

/// <summary>
/// İki adımlı doğrulama kurulumunu başlatır: doğrulayıcı uygulamaya (Microsoft/Google Authenticator…) eklenecek
/// yeni bir anahtar üretir. Doğrulama, EnableTwoFactorCommand ile ilk kod girilince açılır.
/// </summary>
public sealed record SetupTwoFactorCommand : ICommand<TwoFactorSetupResponse>;

internal sealed class SetupTwoFactorCommandHandler(IIdentityService identityService, IUserContext userContext)
    : ICommandHandler<SetupTwoFactorCommand, TwoFactorSetupResponse>
{
    public Task<Result<TwoFactorSetupResponse>> Handle(SetupTwoFactorCommand command, CancellationToken cancellationToken) =>
        userContext.UserId is { } userId
            ? identityService.SetupTwoFactorAsync(userId, cancellationToken)
            : Task.FromResult<Result<TwoFactorSetupResponse>>(UserContextErrors.NotAuthenticated);
}
//#endif
