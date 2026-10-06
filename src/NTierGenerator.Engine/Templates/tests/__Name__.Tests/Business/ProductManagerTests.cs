//#if Sample
using System.Linq.Expressions;
using __Name__.Business.Abstract;
using __Name__.Business.Concrete;
using __Name__.Business.Constants;
using __Name__.Core.Utilities.Results;
using __Name__.DataAccess.Abstract;
using __Name__.Entities.Concrete;
using Moq;

namespace __Name__.Tests.Business;

/// <summary>
/// İş kuralları, veritabanı yerine sahte (mock) Dal ile test edilir.
/// Aspect'ler yalnızca Autofac proxy'si üzerinden çalıştığı için burada devreye girmez.
/// </summary>
public class ProductManagerTests
{
    private readonly Mock<IProductDal> _productDal = new();
    private readonly Mock<ICategoryService> _categoryService = new();
    private readonly ProductManager _productManager;

    public ProductManagerTests()
    {
        _productManager = new ProductManager(_productDal.Object, _categoryService.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ProductDoesNotExist_ReturnsError()
    {
        _productDal
            .Setup(dal => dal.GetAsync(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var result = await _productManager.GetByIdAsync(42);

        Assert.False(result.Success);
        Assert.Equal(Messages.ProductNotFound, result.Message);
    }

    [Fact]
    public async Task AddAsync_ProductNameAlreadyExists_ReturnsErrorAndDoesNotAdd()
    {
        _productDal
            .Setup(dal => dal.AnyAsync(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _productManager.AddAsync(new Product { Name = "Klavye", CategoryId = 1, UnitPrice = 750 });

        Assert.False(result.Success);
        Assert.Equal(Messages.ProductNameAlreadyExists, result.Message);
        _productDal.Verify(dal => dal.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddAsync_ValidProduct_AddsProduct()
    {
        var product = new Product { Name = "Klavye", CategoryId = 1, UnitPrice = 750 };
        _productDal
            .Setup(dal => dal.AnyAsync(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _productDal
            .Setup(dal => dal.CountAsync(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _productDal
            .Setup(dal => dal.AddAsync(product, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        _categoryService
            .Setup(service => service.GetByIdAsync(1))
            .ReturnsAsync(new SuccessDataResult<Category>(new Category { Id = 1, Name = "Elektronik" }));

        var result = await _productManager.AddAsync(product);

        Assert.True(result.Success);
        Assert.Equal(Messages.ProductAdded, result.Message);
        _productDal.Verify(dal => dal.AddAsync(product, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddAsync_CategoryProductLimitReached_ReturnsError()
    {
        _productDal
            .Setup(dal => dal.AnyAsync(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _productDal
            .Setup(dal => dal.CountAsync(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProductManager.MaxProductCountPerCategory);
        _categoryService
            .Setup(service => service.GetByIdAsync(1))
            .ReturnsAsync(new SuccessDataResult<Category>(new Category { Id = 1, Name = "Elektronik" }));

        var result = await _productManager.AddAsync(new Product { Name = "Fare", CategoryId = 1, UnitPrice = 300 });

        Assert.False(result.Success);
        Assert.Equal(Messages.CategoryProductLimitExceeded(ProductManager.MaxProductCountPerCategory), result.Message);
    }
}
//#endif
