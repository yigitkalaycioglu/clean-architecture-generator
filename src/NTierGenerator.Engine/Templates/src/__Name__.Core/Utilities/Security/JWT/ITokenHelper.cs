//#if Auth
using __Name__.Core.Entities.Concrete;

namespace __Name__.Core.Utilities.Security.JWT;

public interface ITokenHelper
{
    AccessToken CreateToken(User user, IEnumerable<OperationClaim> operationClaims);
}
//#endif
