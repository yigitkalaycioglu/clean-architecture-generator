using System.Security.Claims;
using __Name__.Application.Abstractions.Authentication;

namespace __Name__.Api.Common;

/// <summary>İsteği yapan kullanıcının kimliği, doğrulanmış erişim token'ındaki "sub" talebinden okunur.</summary>
internal sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : IUserContext
{
    public string? UserId =>
        httpContextAccessor.HttpContext?.User is { Identity.IsAuthenticated: true } user
            ? user.FindFirstValue("sub")
            : null;
}
