//#if Auth
using __Name__.Business.Abstract;
using __Name__.Entities.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace __Name__.WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(UserForRegisterDto userForRegisterDto)
    {
        var result = await authService.RegisterAsync(userForRegisterDto);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(UserForLoginDto userForLoginDto)
    {
        var result = await authService.LoginAsync(userForLoginDto);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
//#endif
