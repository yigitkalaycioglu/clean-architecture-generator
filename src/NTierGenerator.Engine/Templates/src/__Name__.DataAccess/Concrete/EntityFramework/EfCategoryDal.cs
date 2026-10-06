//#if Sample
using __Name__.Core.DataAccess.EntityFramework;
using __Name__.DataAccess.Abstract;
using __Name__.DataAccess.Concrete.EntityFramework.Contexts;
using __Name__.Entities.Concrete;
using Microsoft.EntityFrameworkCore;

namespace __Name__.DataAccess.Concrete.EntityFramework;

public class EfCategoryDal(__ContextName__ context) : EfEntityRepositoryBase<Category, __ContextName__>(context), ICategoryDal
{
    public Task<bool> HasProductsAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        return Context.Products.AnyAsync(product => product.CategoryId == categoryId, cancellationToken);
    }
}
//#endif
