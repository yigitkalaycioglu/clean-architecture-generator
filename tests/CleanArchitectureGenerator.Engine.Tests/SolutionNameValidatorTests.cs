using CleanArchitectureGenerator.Engine.Validation;

namespace CleanArchitectureGenerator.Engine.Tests;

public class SolutionNameValidatorTests
{
    [Theory]
    [InlineData("Northwind")]
    [InlineData("Acme.ECommerce")]
    [InlineData("My_Company.Project2")]
    [InlineData("_Internal.Tools")]
    public void Validate_ValidNames_ReturnsValid(string name)
    {
        Assert.True(SolutionNameValidator.Validate(name).IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("My App")]
    [InlineData("1Project")]
    [InlineData("Acme..Shop")]
    [InlineData(".Acme")]
    [InlineData("Acme.")]
    [InlineData("Acme-Shop")]
    [InlineData("Acme.class")]
    [InlineData("CON")]
    [InlineData("System.Tools")]
    [InlineData("Mağaza")]
    [InlineData("Acme.Result")]
    [InlineData("TodoItem.Api")]
    [InlineData("Acme.Task")]
    public void Validate_InvalidNames_ReturnsError(string name)
    {
        var result = SolutionNameValidator.Validate(name);

        Assert.False(result.IsValid);
        Assert.False(string.IsNullOrWhiteSpace(result.Error));
    }

    [Theory]
    [InlineData("Mağaza Yönetimi", "MagazaYonetimi")]
    [InlineData("şirket.stok takip", "Sirket.StokTakip")]
    [InlineData("acme-shop", "AcmeShop")]
    public void SuggestName_ProducesValidName(string input, string expected)
    {
        Assert.Equal(expected, SolutionNameValidator.SuggestName(input));
    }
}
