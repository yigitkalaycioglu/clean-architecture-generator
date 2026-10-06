//#if Auth
using __Name__.Core.Utilities.Results;
using __Name__.Core.Utilities.Security.JWT;
using __Name__.Entities.DTOs;

namespace __Name__.Business.Abstract;

public interface IAuthService
{
    Task<IDataResult<AccessToken>> RegisterAsync(UserForRegisterDto userForRegisterDto);

    Task<IDataResult<AccessToken>> LoginAsync(UserForLoginDto userForLoginDto);
}
//#endif
