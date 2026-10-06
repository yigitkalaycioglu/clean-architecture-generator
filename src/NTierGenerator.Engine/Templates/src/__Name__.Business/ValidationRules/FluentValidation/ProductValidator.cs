//#if Sample
using __Name__.Entities.Concrete;
using FluentValidation;

namespace __Name__.Business.ValidationRules.FluentValidation;

public class ProductValidator : AbstractValidator<Product>
{
    public ProductValidator()
    {
        RuleFor(product => product.Name).NotEmpty().Length(2, 150);
        RuleFor(product => product.CategoryId).GreaterThan(0);
        RuleFor(product => product.UnitPrice).GreaterThan(0);
        RuleFor(product => product.UnitsInStock).GreaterThanOrEqualTo(0);
    }
}
//#endif
