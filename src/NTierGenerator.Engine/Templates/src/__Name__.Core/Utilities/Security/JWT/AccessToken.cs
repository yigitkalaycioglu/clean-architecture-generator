//#if Auth
namespace __Name__.Core.Utilities.Security.JWT;

/// <param name="Token">İmzalı JWT.</param>
/// <param name="Expiration">Geçerlilik bitişi (UTC).</param>
public sealed record AccessToken(string Token, DateTime Expiration);
//#endif
