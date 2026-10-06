//#if Auth
namespace __Name__.Core.Entities.Concrete;

/// <summary>
/// Kullanıcı ile yetki arasındaki çoka-çok ilişki.
/// </summary>
public class UserOperationClaim : BaseEntity
{
    public int UserId { get; set; }

    public int OperationClaimId { get; set; }
}
//#endif
