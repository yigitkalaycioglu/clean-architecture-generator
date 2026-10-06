//#if Sample
using __Name__.Core.Entities;

namespace __Name__.Entities.DTOs;

/// <summary>
/// Ürünü kategori adıyla birlikte taşıyan, iki tablonun birleşiminden oluşan DTO.
/// </summary>
public sealed class ProductDetailDto : IDto
{
    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string CategoryName { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }

    public int UnitsInStock { get; set; }
}
//#endif
