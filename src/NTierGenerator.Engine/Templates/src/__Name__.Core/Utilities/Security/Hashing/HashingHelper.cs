//#if Auth
using System.Security.Cryptography;

namespace __Name__.Core.Utilities.Security.Hashing;

/// <summary>
/// Parolaları PBKDF2 (HMAC-SHA512, 210.000 tekrar, 128 bit salt) ile özetler.
/// Karşılaştırma sabit sürede yapılır (timing saldırılarına karşı).
/// </summary>
public static class HashingHelper
{
    private const int SaltSize = 16;
    private const int HashSize = 64;
    private const int Iterations = 210_000;

    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA512;

    public static void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
    {
        passwordSalt = RandomNumberGenerator.GetBytes(SaltSize);
        passwordHash = Rfc2898DeriveBytes.Pbkdf2(password, passwordSalt, Iterations, Algorithm, HashSize);
    }

    public static bool VerifyPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt)
    {
        var computedHash = Rfc2898DeriveBytes.Pbkdf2(password, passwordSalt, Iterations, Algorithm, passwordHash.Length);
        return CryptographicOperations.FixedTimeEquals(computedHash, passwordHash);
    }
}
//#endif
