//#if Auth
using __Name__.Core.Entities;

namespace __Name__.Entities.DTOs;

public sealed class UserForRegisterDto : IDto
{
    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;
}
//#endif
