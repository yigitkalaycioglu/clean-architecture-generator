//#if Auth
using System.Security.Claims;

namespace __Name__.Core.Extensions;

/// <summary>
/// Giriş yapmış kullanıcının (HttpContext.User) claim'lerini okumak için yardımcılar.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    public static List<string> GetClaimValues(this ClaimsPrincipal principal, string claimType) =>
        principal.FindAll(claimType).Select(claim => claim.Value).ToList();

    public static List<string> GetRoles(this ClaimsPrincipal principal) =>
        principal.GetClaimValues(ClaimTypes.Role);

    public static int? GetUserId(this ClaimsPrincipal principal) =>
        int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ? userId : null;
}
//#endif
