//#if Auth
using __Name__.Entities.DTOs;
using FluentValidation;

namespace __Name__.Business.ValidationRules.FluentValidation;

public class UserForLoginDtoValidator : AbstractValidator<UserForLoginDto>
{
    public UserForLoginDtoValidator()
    {
        RuleFor(user => user.Email).NotEmpty().EmailAddress();
        RuleFor(user => user.Password).NotEmpty();
    }
}
//#endif
