//#if Auth
using __Name__.Core.Utilities.Security.Hashing;

namespace __Name__.Tests.Core;

public class HashingHelperTests
{
    [Fact]
    public void VerifyPasswordHash_CorrectPassword_ReturnsTrue()
    {
        HashingHelper.CreatePasswordHash("Parola123!", out var hash, out var salt);

        Assert.True(HashingHelper.VerifyPasswordHash("Parola123!", hash, salt));
    }

    [Fact]
    public void VerifyPasswordHash_WrongPassword_ReturnsFalse()
    {
        HashingHelper.CreatePasswordHash("Parola123!", out var hash, out var salt);

        Assert.False(HashingHelper.VerifyPasswordHash("parola123!", hash, salt));
    }

    [Fact]
    public void CreatePasswordHash_SamePassword_UsesDifferentSalts()
    {
        HashingHelper.CreatePasswordHash("Parola123!", out var firstHash, out var firstSalt);
        HashingHelper.CreatePasswordHash("Parola123!", out var secondHash, out var secondSalt);

        Assert.NotEqual(firstSalt, secondSalt);
        Assert.NotEqual(firstHash, secondHash);
    }
}
//#endif
