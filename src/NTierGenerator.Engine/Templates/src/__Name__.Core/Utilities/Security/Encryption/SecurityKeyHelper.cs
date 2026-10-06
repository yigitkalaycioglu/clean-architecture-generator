//#if Auth
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace __Name__.Core.Utilities.Security.Encryption;

public static class SecurityKeyHelper
{
    /// <summary>HS512 imzası en az 512 bit (64 bayt) anahtar ister.</summary>
    private const int MinimumKeyBytes = 64;

    public static SecurityKey CreateSecurityKey(string securityKey)
    {
        if (string.IsNullOrWhiteSpace(securityKey) || Encoding.UTF8.GetByteCount(securityKey) < MinimumKeyBytes)
        {
            throw new InvalidOperationException(
                $"TokenOptions:SecurityKey ayarı en az {MinimumKeyBytes} bayt uzunluğunda olmalıdır (appsettings.json ya da ortam değişkeni).");
        }

        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(securityKey));
    }
}
//#endif
