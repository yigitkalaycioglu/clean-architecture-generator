//#if Auth
using __Name__.Core.Entities;

namespace __Name__.Entities.DTOs;

public sealed class UserForLoginDto : IDto
{
    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}
//#endif
