//#if Sample
using __Name__.Business.Abstract;
//#if Auth
using __Name__.Business.BusinessAspects.Autofac;
//#endif
using __Name__.Business.Constants;
using __Name__.Business.ValidationRules.FluentValidation;
using __Name__.Core.Aspects.Autofac.Caching;
using __Name__.Core.Aspects.Autofac.Validation;
using __Name__.Core.Utilities.Business;
using __Name__.Core.Utilities.Results;
using __Name__.DataAccess.Abstract;
using __Name__.Entities.Concrete;

namespace __Name__.Business.Concrete;

public class CategoryManager(ICategoryDal categoryDal) : ICategoryService
{
    [CacheAspect(duration: 30)]
    public async Task<IDataResult<List<Category>>> GetAllAsync()
    {
        return new SuccessDataResult<List<Category>>(await categoryDal.GetListAsync());
    }

    public async Task<IDataResult<Category>> GetByIdAsync(int id)
    {
        var category = await categoryDal.GetAsync(c => c.Id == id);
        return category is null
            ? new ErrorDataResult<Category>(Messages.CategoryNotFound)
            : new SuccessDataResult<Category>(category);
    }

//#if Auth
    [SecuredOperation("admin")]
//#endif
    [ValidationAspect(typeof(CategoryValidator))]
    [CacheRemoveAspect("ICategoryService.Get")]
    public async Task<IResult> AddAsync(Category category)
    {
        var ruleResult = await BusinessRules.RunAsync(() => CheckIfCategoryNameIsUniqueAsync(category.Name));
        if (ruleResult is not null)
        {
            return ruleResult;
        }

        await categoryDal.AddAsync(category);
        return new SuccessResult(Messages.CategoryAdded);
    }

//#if Auth
    [SecuredOperation("admin")]
//#endif
    [ValidationAspect(typeof(CategoryValidator))]
    [CacheRemoveAspect("ICategoryService.Get")]
    [CacheRemoveAspect("IProductService.GetProductDetails")]
    public async Task<IResult> UpdateAsync(Category category)
    {
        if (!await categoryDal.AnyAsync(c => c.Id == category.Id))
        {
            return new ErrorResult(Messages.CategoryNotFound);
        }

        var ruleResult = await BusinessRules.RunAsync(() => CheckIfCategoryNameIsUniqueAsync(category.Name, category.Id));
        if (ruleResult is not null)
        {
            return ruleResult;
        }

        await categoryDal.UpdateAsync(category);
        return new SuccessResult(Messages.CategoryUpdated);
    }

//#if Auth
    [SecuredOperation("admin")]
//#endif
    [CacheRemoveAspect("ICategoryService.Get")]
    public async Task<IResult> DeleteAsync(int id)
    {
        var category = await categoryDal.GetAsync(c => c.Id == id);
        if (category is null)
        {
            return new ErrorResult(Messages.CategoryNotFound);
        }

        if (await categoryDal.HasProductsAsync(id))
        {
            return new ErrorResult(Messages.CategoryHasProducts);
        }

        await categoryDal.DeleteAsync(category);
        return new SuccessResult(Messages.CategoryDeleted);
    }

    private async Task<IResult> CheckIfCategoryNameIsUniqueAsync(string categoryName, int excludedCategoryId = 0)
    {
        var exists = await categoryDal.AnyAsync(c => c.Name == categoryName && c.Id != excludedCategoryId);
        return exists ? new ErrorResult(Messages.CategoryNameAlreadyExists) : new SuccessResult();
    }
}
//#endif
