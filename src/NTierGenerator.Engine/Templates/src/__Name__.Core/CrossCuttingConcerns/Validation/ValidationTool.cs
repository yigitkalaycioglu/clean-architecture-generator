using FluentValidation;

namespace __Name__.Core.CrossCuttingConcerns.Validation;

public static class ValidationTool
{
    /// <summary>Nesneyi doğrular; geçersizse tüm hataları içeren <see cref="ValidationException"/> fırlatır.</summary>
    public static void Validate(IValidator validator, object instance)
    {
        var result = validator.Validate(new ValidationContext<object>(instance));
        if (!result.IsValid)
        {
            throw new ValidationException(result.Errors);
        }
    }
}
