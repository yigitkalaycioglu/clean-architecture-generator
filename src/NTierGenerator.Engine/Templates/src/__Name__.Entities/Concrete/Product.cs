//#if Sample
using __Name__.Core.Entities;

namespace __Name__.Entities.Concrete;

public class Product : BaseEntity
{
    public int CategoryId { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }

    public int UnitsInStock { get; set; }

    public Category? Category { get; set; }
}
//#endif
