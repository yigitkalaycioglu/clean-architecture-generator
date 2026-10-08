using __Name__.Api.Common;
using __Name__.Application.Abstractions.Messaging;
using __Name__.Application.Account;

namespace __Name__.Api.Endpoints;

/// <summary>Oturum açmış kullanıcının kendi hesabı. Tüm uç noktalar kimlik doğrulaması ister.</summary>
public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/account")
            .WithTags("Account")
            .RequireAuthorization();

        group.MapGet("/", async (ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetAccountQuery(), cancellationToken);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
        })
        .Produces<AccountResponse>();
//#if LocalAuth

        // Parola gerektiren işlemler sıkı hız sınırına tabidir: çalınmış bir oturumla parola tahmin edilemez.
        group.MapPost("/change-password", async (ChangePasswordCommand command, ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.IsSuccess ? Results.NoContent() : result.ToProblem();
        })
        .RequireRateLimiting(RateLimitPolicies.Authentication);

        group.MapPost("/2fa/setup", async (ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new SetupTwoFactorCommand(), cancellationToken);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
        })
        .Produces<TwoFactorSetupResponse>()
        .RequireRateLimiting(RateLimitPolicies.Authentication);

        group.MapPost("/2fa/enable", async (EnableTwoFactorCommand command, ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
        })
        .Produces<RecoveryCodesResponse>()
        .RequireRateLimiting(RateLimitPolicies.Authentication);

        group.MapPost("/2fa/disable", async (DisableTwoFactorCommand command, ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.IsSuccess ? Results.NoContent() : result.ToProblem();
        })
        .RequireRateLimiting(RateLimitPolicies.Authentication);

        group.MapPost("/2fa/recovery-codes", async (RegenerateRecoveryCodesCommand command, ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
        })
        .Produces<RecoveryCodesResponse>()
        .RequireRateLimiting(RateLimitPolicies.Authentication);
//#endif

        return app;
    }
}
