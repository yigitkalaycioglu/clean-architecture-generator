//#if Sample
using __Name__.Core.Entities;

namespace __Name__.Entities.Concrete;

public class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ICollection<Product> Products { get; set; } = [];
}
//#endif
