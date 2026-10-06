//#if Auth
using __Name__.Entities.DTOs;
using FluentValidation;

namespace __Name__.Business.ValidationRules.FluentValidation;

public class UserForRegisterDtoValidator : AbstractValidator<UserForRegisterDto>
{
    public UserForRegisterDtoValidator()
    {
        RuleFor(user => user.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(user => user.Password).NotEmpty().MinimumLength(8).MaximumLength(128);
        RuleFor(user => user.FirstName).NotEmpty().MaximumLength(50);
        RuleFor(user => user.LastName).NotEmpty().MaximumLength(50);
    }
}
//#endif
