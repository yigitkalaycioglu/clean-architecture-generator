using __Name__.Domain.Common;

namespace __Name__.UnitTests.Domain;

public class ResultTests
{
    [Fact]
    public void Success_HasNoError()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.Equal(Error.None, result.Error);
    }

    [Fact]
    public void Failure_ExposesErrorAndHidesValue()
    {
        var error = Error.NotFound("Test.NotFound", "Kayıt bulunamadı.");

        Result<int> result = error;

        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void Failure_WithoutError_IsRejected()
    {
        Assert.Throws<ArgumentException>(() => Result.Failure(Error.None));
    }

    [Fact]
    public void Value_ConvertsToSuccessfulResult()
    {
        Result<string> result = "değer";

        Assert.True(result.IsSuccess);
        Assert.Equal("değer", result.Value);
    }
}
