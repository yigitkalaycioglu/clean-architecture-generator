using __Name__.Domain.Common;

namespace __Name__.Application.Abstractions.Authentication;

/// <summary>
/// İsteği yapan kullanıcı. Kimlik yalnızca doğrulanmış token'daki "sub" değerinden okunur; istemcinin gövdede
/// ya da adreste gönderdiği bir kullanıcı kimliğine asla güvenilmez.
/// </summary>
public interface IUserContext
{
    /// <summary>Oturum açmış kullanıcının kimliği; anonim istekte null.</summary>
    string? UserId { get; }
}

public static class UserContextErrors
{
    public static readonly Error NotAuthenticated = Error.Unauthorized("User.NotAuthenticated", "Bu işlem için oturum açmalısınız.");
}
