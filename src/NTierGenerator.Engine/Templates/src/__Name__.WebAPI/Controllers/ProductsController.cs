//#if Sample
using __Name__.Business.Abstract;
using __Name__.Entities.Concrete;
using Microsoft.AspNetCore.Mvc;

namespace __Name__.WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ProductsController(IProductService productService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await productService.GetAllAsync();
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await productService.GetByIdAsync(id);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpGet("by-category/{categoryId:int}")]
    public async Task<IActionResult> GetAllByCategoryId(int categoryId)
    {
        var result = await productService.GetAllByCategoryIdAsync(categoryId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("details")]
    public async Task<IActionResult> GetProductDetails()
    {
        var result = await productService.GetProductDetailsAsync();
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost]
    public async Task<IActionResult> Add(Product product)
    {
        var result = await productService.AddAsync(product);
        return result.Success ? CreatedAtAction(nameof(GetById), new { id = product.Id }, result) : BadRequest(result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, Product product)
    {
        product.Id = id;
        var result = await productService.UpdateAsync(product);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await productService.DeleteAsync(id);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
//#endif
