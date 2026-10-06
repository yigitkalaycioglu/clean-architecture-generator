//#if Sample
using __Name__.Core.DataAccess;
using __Name__.Entities.Concrete;
using __Name__.Entities.DTOs;

namespace __Name__.DataAccess.Abstract;

public interface IProductDal : IEntityRepository<Product>
{
    Task<List<ProductDetailDto>> GetProductDetailsAsync(CancellationToken cancellationToken = default);
}
//#endif
