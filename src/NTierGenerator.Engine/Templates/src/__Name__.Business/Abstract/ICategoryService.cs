//#if Sample
using __Name__.Core.Utilities.Results;
using __Name__.Entities.Concrete;

namespace __Name__.Business.Abstract;

public interface ICategoryService
{
    Task<IDataResult<List<Category>>> GetAllAsync();

    Task<IDataResult<Category>> GetByIdAsync(int id);

    Task<IResult> AddAsync(Category category);

    Task<IResult> UpdateAsync(Category category);

    Task<IResult> DeleteAsync(int id);
}
//#endif
