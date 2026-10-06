//#if Auth
using __Name__.Core.DataAccess.EntityFramework;
using __Name__.Core.Entities.Concrete;
using __Name__.DataAccess.Abstract;
using __Name__.DataAccess.Concrete.EntityFramework.Contexts;
using Microsoft.EntityFrameworkCore;

namespace __Name__.DataAccess.Concrete.EntityFramework;

public class EfUserDal(__ContextName__ context) : EfEntityRepositoryBase<User, __ContextName__>(context), IUserDal
{
    public Task<List<OperationClaim>> GetClaimsAsync(User user, CancellationToken cancellationToken = default)
    {
        var query =
            from operationClaim in Context.OperationClaims
            join userOperationClaim in Context.UserOperationClaims on operationClaim.Id equals userOperationClaim.OperationClaimId
            where userOperationClaim.UserId == user.Id
            select operationClaim;

        return query.AsNoTracking().ToListAsync(cancellationToken);
    }
}
//#endif
