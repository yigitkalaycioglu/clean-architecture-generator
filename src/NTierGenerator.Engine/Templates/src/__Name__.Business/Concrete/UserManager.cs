//#if Auth
using __Name__.Business.Abstract;
using __Name__.Business.Constants;
using __Name__.Core.Entities.Concrete;
using __Name__.Core.Utilities.Results;
using __Name__.DataAccess.Abstract;

namespace __Name__.Business.Concrete;

public class UserManager(IUserDal userDal) : IUserService
{
    public async Task<IDataResult<User>> GetByEmailAsync(string email)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await userDal.GetAsync(u => u.Email == normalizedEmail);
        return user is null
            ? new ErrorDataResult<User>(Messages.UserNotFound)
            : new SuccessDataResult<User>(user);
    }

    public async Task<IDataResult<List<OperationClaim>>> GetClaimsAsync(User user)
    {
        return new SuccessDataResult<List<OperationClaim>>(await userDal.GetClaimsAsync(user));
    }

    public async Task<IResult> AddAsync(User user)
    {
        await userDal.AddAsync(user);
        return new SuccessResult();
    }
}
//#endif
