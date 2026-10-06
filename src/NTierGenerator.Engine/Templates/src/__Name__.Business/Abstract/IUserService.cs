//#if Auth
using __Name__.Core.Entities.Concrete;
using __Name__.Core.Utilities.Results;

namespace __Name__.Business.Abstract;

public interface IUserService
{
    Task<IDataResult<User>> GetByEmailAsync(string email);

    Task<IDataResult<List<OperationClaim>>> GetClaimsAsync(User user);

    Task<IResult> AddAsync(User user);
}
//#endif
