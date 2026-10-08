//#if LocalAuth
using __Name__.Application.Authentication;

namespace __Name__.UnitTests.Authentication;

public class RegisterCommandValidatorTests
{
    private readonly RegisterCommandValidator _validator = new();

    [Fact]
    public void LongPassword_Passes()
    {
        var result = _validator.Validate(new RegisterCommand("ornek@ornek.com", "uzun-ve-tahmin-edilemez"));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("kisa-parola")]
    [InlineData("")]
    public void ShortPassword_Fails(string password)
    {
        var result = _validator.Validate(new RegisterCommand("ornek@ornek.com", password));

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(RegisterCommand.Password));
    }

    [Fact]
    public void TooLongPassword_Fails()
    {
        var password = new string('a', AuthenticationRules.PasswordMaxLength + 1);

        var result = _validator.Validate(new RegisterCommand("ornek@ornek.com", password));

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(RegisterCommand.Password));
    }

    [Theory]
    [InlineData("")]
    [InlineData("e-posta-degil")]
    public void InvalidEmail_Fails(string email)
    {
        var result = _validator.Validate(new RegisterCommand(email, "uzun-ve-tahmin-edilemez"));

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(RegisterCommand.Email));
    }
}
//#endif
