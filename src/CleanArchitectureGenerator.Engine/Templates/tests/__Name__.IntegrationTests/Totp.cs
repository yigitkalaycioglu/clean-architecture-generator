//#if LocalAuth
using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;

namespace __Name__.IntegrationTests;

/// <summary>Doğrulayıcı uygulamaların ürettiği 6 haneli kodu (TOTP, RFC 6238) testte üretir.</summary>
internal static class Totp
{
    private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    public static string Compute(string sharedKey)
    {
        var key = DecodeBase32(sharedKey.Replace(" ", string.Empty, StringComparison.Ordinal).ToUpperInvariant());
        Span<byte> counter = stackalloc byte[8];
        BinaryPrimitives.WriteInt64BigEndian(counter, DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30);

#pragma warning disable CA5350 // RFC 6238 ve doğrulayıcı uygulamalar HMAC-SHA1 kullanır; burada yalnızca test kodu üretilir.
        var hash = HMACSHA1.HashData(key, counter);
#pragma warning restore CA5350

        var offset = hash[^1] & 0x0F;
        var value = BinaryPrimitives.ReadInt32BigEndian(hash.AsSpan(offset)) & 0x7FFFFFFF;
        return (value % 1_000_000).ToString("D6", CultureInfo.InvariantCulture);
    }

    private static byte[] DecodeBase32(string input)
    {
        var output = new List<byte>(input.Length * 5 / 8);
        int buffer = 0, bits = 0;
        foreach (var character in input.TrimEnd('='))
        {
            buffer = (buffer << 5) | Base32Alphabet.IndexOf(character, StringComparison.Ordinal);
            bits += 5;
            if (bits >= 8)
            {
                bits -= 8;
                output.Add((byte)(buffer >> bits));
                buffer &= (1 << bits) - 1;
            }
        }

        return [.. output];
    }
}
//#endif
