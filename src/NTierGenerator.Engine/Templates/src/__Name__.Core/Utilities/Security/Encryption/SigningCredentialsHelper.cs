//#if Auth
using Microsoft.IdentityModel.Tokens;

namespace __Name__.Core.Utilities.Security.Encryption;

public static class SigningCredentialsHelper
{
    public static SigningCredentials CreateSigningCredentials(SecurityKey securityKey) =>
        new(securityKey, SecurityAlgorithms.HmacSha512);
}
//#endif
