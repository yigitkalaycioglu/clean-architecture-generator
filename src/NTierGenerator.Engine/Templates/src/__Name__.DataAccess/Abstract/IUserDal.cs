//#if Auth
using __Name__.Core.DataAccess;
using __Name__.Core.Entities.Concrete;

namespace __Name__.DataAccess.Abstract;

public interface IUserDal : IEntityRepository<User>
{
    Task<List<OperationClaim>> GetClaimsAsync(User user, CancellationToken cancellationToken = default);
}
//#endif
