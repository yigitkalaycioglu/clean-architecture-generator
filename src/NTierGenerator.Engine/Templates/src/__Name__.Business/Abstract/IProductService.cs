//#if Sample
using __Name__.Core.Utilities.Results;
using __Name__.Entities.Concrete;
using __Name__.Entities.DTOs;

namespace __Name__.Business.Abstract;

public interface IProductService
{
    Task<IDataResult<List<Product>>> GetAllAsync();

    Task<IDataResult<List<Product>>> GetAllByCategoryIdAsync(int categoryId);

    Task<IDataResult<List<ProductDetailDto>>> GetProductDetailsAsync();

    Task<IDataResult<Product>> GetByIdAsync(int id);

    Task<IResult> AddAsync(Product product);

    Task<IResult> UpdateAsync(Product product);

    Task<IResult> DeleteAsync(int id);
}
//#endif
