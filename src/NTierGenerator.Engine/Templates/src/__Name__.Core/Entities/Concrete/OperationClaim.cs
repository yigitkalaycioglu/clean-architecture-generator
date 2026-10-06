//#if Auth
namespace __Name__.Core.Entities.Concrete;

/// <summary>
/// Yetki (rol). Örneğin "admin", "product.add". SecuredOperation aspect'i bu adları kontrol eder.
/// </summary>
public class OperationClaim : BaseEntity
{
    public string Name { get; set; } = string.Empty;
}
//#endif
