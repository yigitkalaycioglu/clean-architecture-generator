using __Name__.Application.Abstractions.Authentication;
using __Name__.Application.Abstractions.Messaging;
using __Name__.Domain.Common;

namespace __Name__.Application.Account;

/// <summary>Oturum açmış kullanıcının hesap bilgileri.</summary>
public sealed record GetAccountQuery : IQuery<AccountResponse>;

//#if LocalAuth
internal sealed class GetAccountQueryHandler(IIdentityService identityService, IUserContext userContext)
    : IQueryHandler<GetAccountQuery, AccountResponse>
{
    public Task<Result<AccountResponse>> Handle(GetAccountQuery query, CancellationToken cancellationToken) =>
        userContext.UserId is { } userId
            ? identityService.GetAccountAsync(userId, cancellationToken)
            : Task.FromResult<Result<AccountResponse>>(UserContextErrors.NotAuthenticated);
}
//#else
internal sealed class GetAccountQueryHandler(IUserContext userContext) : IQueryHandler<GetAccountQuery, AccountResponse>
{
    public Task<Result<AccountResponse>> Handle(GetAccountQuery query, CancellationToken cancellationToken)
    {
        Result<AccountResponse> result = userContext.UserId is { } userId
            ? new AccountResponse(userId)
            : UserContextErrors.NotAuthenticated;

        return Task.FromResult(result);
    }
}
//#endif
