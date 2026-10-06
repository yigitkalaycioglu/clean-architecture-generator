//#if Sample
using __Name__.Core.DataAccess.EntityFramework;
using __Name__.DataAccess.Abstract;
using __Name__.DataAccess.Concrete.EntityFramework.Contexts;
using __Name__.Entities.Concrete;
using __Name__.Entities.DTOs;
using Microsoft.EntityFrameworkCore;

namespace __Name__.DataAccess.Concrete.EntityFramework;

public class EfProductDal(__ContextName__ context) : EfEntityRepositoryBase<Product, __ContextName__>(context), IProductDal
{
    /// <summary>Ürünleri kategori adıyla birlikte tek sorguda (JOIN) getirir.</summary>
    public Task<List<ProductDetailDto>> GetProductDetailsAsync(CancellationToken cancellationToken = default)
    {
        return Context.Products
            .AsNoTracking()
            .OrderBy(product => product.Name)
            .Select(product => new ProductDetailDto
            {
                ProductId = product.Id,
                ProductName = product.Name,
                CategoryName = product.Category!.Name,
                UnitPrice = product.UnitPrice,
                UnitsInStock = product.UnitsInStock
            })
            .ToListAsync(cancellationToken);
    }
}
//#endif
