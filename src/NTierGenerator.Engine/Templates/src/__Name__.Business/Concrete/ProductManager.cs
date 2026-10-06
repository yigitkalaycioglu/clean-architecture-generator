//#if Sample
using __Name__.Business.Abstract;
//#if Auth
using __Name__.Business.BusinessAspects.Autofac;
//#endif
using __Name__.Business.Constants;
using __Name__.Business.ValidationRules.FluentValidation;
using __Name__.Core.Aspects.Autofac.Caching;
using __Name__.Core.Aspects.Autofac.Performance;
using __Name__.Core.Aspects.Autofac.Validation;
using __Name__.Core.Utilities.Business;
using __Name__.Core.Utilities.Results;
using __Name__.DataAccess.Abstract;
using __Name__.Entities.Concrete;
using __Name__.Entities.DTOs;

namespace __Name__.Business.Concrete;

/// <summary>
/// Ürün iş kuralları. Doğrulama, cache ve yetki kontrolü attribute (aspect) olarak eklenir;
/// metot gövdelerinde yalnızca iş mantığı kalır.
/// </summary>
public class ProductManager(IProductDal productDal, ICategoryService categoryService) : IProductService
{
    public const int MaxProductCountPerCategory = 50;

    [CacheAspect(duration: 10)]
    public async Task<IDataResult<List<Product>>> GetAllAsync()
    {
        return new SuccessDataResult<List<Product>>(await productDal.GetListAsync(), Messages.ProductsListed);
    }

    [CacheAspect(duration: 10)]
    public async Task<IDataResult<List<Product>>> GetAllByCategoryIdAsync(int categoryId)
    {
        return new SuccessDataResult<List<Product>>(await productDal.GetListAsync(p => p.CategoryId == categoryId));
    }

    [CacheAspect(duration: 10)]
    [PerformanceAspect(intervalInSeconds: 3)]
    public async Task<IDataResult<List<ProductDetailDto>>> GetProductDetailsAsync()
    {
        return new SuccessDataResult<List<ProductDetailDto>>(await productDal.GetProductDetailsAsync());
    }

    public async Task<IDataResult<Product>> GetByIdAsync(int id)
    {
        var product = await productDal.GetAsync(p => p.Id == id);
        return product is null
            ? new ErrorDataResult<Product>(Messages.ProductNotFound)
            : new SuccessDataResult<Product>(product);
    }

//#if Auth
    [SecuredOperation("admin,product.add")]
//#endif
    [ValidationAspect(typeof(ProductValidator))]
    [CacheRemoveAspect("IProductService.Get")]
    public async Task<IResult> AddAsync(Product product)
    {
        var ruleResult = await BusinessRules.RunAsync(
            () => CheckIfProductNameIsUniqueAsync(product.Name),
            () => CheckIfCategoryExistsAsync(product.CategoryId),
            () => CheckIfCategoryProductLimitIsNotExceededAsync(product.CategoryId));

        if (ruleResult is not null)
        {
            return ruleResult;
        }

        await productDal.AddAsync(product);
        return new SuccessResult(Messages.ProductAdded);
    }

//#if Auth
    [SecuredOperation("admin")]
//#endif
    [ValidationAspect(typeof(ProductValidator))]
    [CacheRemoveAspect("IProductService.Get")]
    public async Task<IResult> UpdateAsync(Product product)
    {
        if (!await productDal.AnyAsync(p => p.Id == product.Id))
        {
            return new ErrorResult(Messages.ProductNotFound);
        }

        var ruleResult = await BusinessRules.RunAsync(
            () => CheckIfProductNameIsUniqueAsync(product.Name, product.Id),
            () => CheckIfCategoryExistsAsync(product.CategoryId));

        if (ruleResult is not null)
        {
            return ruleResult;
        }

        await productDal.UpdateAsync(product);
        return new SuccessResult(Messages.ProductUpdated);
    }

//#if Auth
    [SecuredOperation("admin")]
//#endif
    [CacheRemoveAspect("IProductService.Get")]
    public async Task<IResult> DeleteAsync(int id)
    {
        var product = await productDal.GetAsync(p => p.Id == id);
        if (product is null)
        {
            return new ErrorResult(Messages.ProductNotFound);
        }

        await productDal.DeleteAsync(product);
        return new SuccessResult(Messages.ProductDeleted);
    }

    private async Task<IResult> CheckIfProductNameIsUniqueAsync(string productName, int excludedProductId = 0)
    {
        var exists = await productDal.AnyAsync(p => p.Name == productName && p.Id != excludedProductId);
        return exists ? new ErrorResult(Messages.ProductNameAlreadyExists) : new SuccessResult();
    }

    private async Task<IResult> CheckIfCategoryExistsAsync(int categoryId)
    {
        var result = await categoryService.GetByIdAsync(categoryId);
        return result.Success ? new SuccessResult() : new ErrorResult(Messages.CategoryNotFound);
    }

    private async Task<IResult> CheckIfCategoryProductLimitIsNotExceededAsync(int categoryId)
    {
        var productCount = await productDal.CountAsync(p => p.CategoryId == categoryId);
        return productCount >= MaxProductCountPerCategory
            ? new ErrorResult(Messages.CategoryProductLimitExceeded(MaxProductCountPerCategory))
            : new SuccessResult();
    }
}
//#endif
