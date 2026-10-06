//#if Sample
using __Name__.Core.DataAccess;
using __Name__.Entities.Concrete;

namespace __Name__.DataAccess.Abstract;

public interface ICategoryDal : IEntityRepository<Category>
{
    Task<bool> HasProductsAsync(int categoryId, CancellationToken cancellationToken = default);
}
//#endif
