//#if Auth
using __Name__.Business.Abstract;
using __Name__.Business.Constants;
using __Name__.Business.ValidationRules.FluentValidation;
using __Name__.Core.Aspects.Autofac.Validation;
using __Name__.Core.Entities.Concrete;
using __Name__.Core.Utilities.Results;
using __Name__.Core.Utilities.Security.Hashing;
using __Name__.Core.Utilities.Security.JWT;
using __Name__.Entities.DTOs;

namespace __Name__.Business.Concrete;

/// <summary>
/// Kayıt ve giriş işlemleri. Başarılı işlemler kullanıcının yetkilerini içeren bir JWT döndürür.
/// </summary>
public class AuthManager(IUserService userService, ITokenHelper tokenHelper) : IAuthService
{
    [ValidationAspect(typeof(UserForRegisterDtoValidator))]
    public async Task<IDataResult<AccessToken>> RegisterAsync(UserForRegisterDto userForRegisterDto)
    {
        var existingUser = await userService.GetByEmailAsync(userForRegisterDto.Email);
        if (existingUser.Success)
        {
            return new ErrorDataResult<AccessToken>(Messages.UserAlreadyExists);
        }

        HashingHelper.CreatePasswordHash(userForRegisterDto.Password, out var passwordHash, out var passwordSalt);
        var user = new User
        {
            Email = userForRegisterDto.Email.Trim().ToLowerInvariant(),
            FirstName = userForRegisterDto.FirstName.Trim(),
            LastName = userForRegisterDto.LastName.Trim(),
            PasswordHash = passwordHash,
            PasswordSalt = passwordSalt,
            Status = true
        };

        await userService.AddAsync(user);
        return await CreateAccessTokenAsync(user, Messages.UserRegistered);
    }

    [ValidationAspect(typeof(UserForLoginDtoValidator))]
    public async Task<IDataResult<AccessToken>> LoginAsync(UserForLoginDto userForLoginDto)
    {
        var user = (await userService.GetByEmailAsync(userForLoginDto.Email)).Data;

        // Kullanıcının bulunamadığı ile parolanın yanlış olduğu ayırt edilmez (hesap tahmini zorlaşır).
        if (user is null || !HashingHelper.VerifyPasswordHash(userForLoginDto.Password, user.PasswordHash, user.PasswordSalt))
        {
            return new ErrorDataResult<AccessToken>(Messages.InvalidCredentials);
        }

        if (!user.Status)
        {
            return new ErrorDataResult<AccessToken>(Messages.UserInactive);
        }

        return await CreateAccessTokenAsync(user, Messages.SuccessfulLogin);
    }

    private async Task<IDataResult<AccessToken>> CreateAccessTokenAsync(User user, string message)
    {
        var claims = await userService.GetClaimsAsync(user);
        var accessToken = tokenHelper.CreateToken(user, claims.Data ?? []);
        return new SuccessDataResult<AccessToken>(accessToken, message);
    }
}
//#endif
