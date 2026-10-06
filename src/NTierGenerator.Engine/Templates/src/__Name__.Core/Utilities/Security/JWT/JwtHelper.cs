//#if Auth
using System.Globalization;
using System.Security.Claims;
using __Name__.Core.Entities.Concrete;
using __Name__.Core.Extensions;
using __Name__.Core.Utilities.Security.Encryption;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace __Name__.Core.Utilities.Security.JWT;

/// <summary>
/// Kullanıcı ve yetkilerinden HS512 ile imzalı JWT üretir.
/// </summary>
public sealed class JwtHelper(IOptions<TokenOptions> tokenOptions) : ITokenHelper
{
    private readonly TokenOptions _tokenOptions = tokenOptions.Value;

    public AccessToken CreateToken(User user, IEnumerable<OperationClaim> operationClaims)
    {
        var now = DateTime.UtcNow;
        var expiration = now.AddMinutes(_tokenOptions.AccessTokenExpiration);
        var securityKey = SecurityKeyHelper.CreateSecurityKey(_tokenOptions.SecurityKey);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Issuer = _tokenOptions.Issuer,
            Audience = _tokenOptions.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expiration,
            Subject = new ClaimsIdentity(CreateClaims(user, operationClaims)),
            SigningCredentials = SigningCredentialsHelper.CreateSigningCredentials(securityKey)
        };

        var token = new JsonWebTokenHandler().CreateToken(tokenDescriptor);
        return new AccessToken(token, expiration);
    }

    private static List<Claim> CreateClaims(User user, IEnumerable<OperationClaim> operationClaims)
    {
        var claims = new List<Claim>();
        claims.AddNameIdentifier(user.Id.ToString(CultureInfo.InvariantCulture));
        claims.AddEmail(user.Email);
        claims.AddName($"{user.FirstName} {user.LastName}");
        claims.AddRoles(operationClaims.Select(claim => claim.Name));
        return claims;
    }
}
//#endif
