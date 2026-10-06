//#if Sample
using __Name__.Entities.Concrete;
using FluentValidation;

namespace __Name__.Business.ValidationRules.FluentValidation;

public class CategoryValidator : AbstractValidator<Category>
{
    public CategoryValidator()
    {
        RuleFor(category => category.Name).NotEmpty().Length(2, 100);
        RuleFor(category => category.Description).MaximumLength(500);
    }
}
//#endif
