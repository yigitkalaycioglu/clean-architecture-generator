//#if Sample
using __Name__.Business.Abstract;
using __Name__.Entities.Concrete;
using Microsoft.AspNetCore.Mvc;

namespace __Name__.WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CategoriesController(ICategoryService categoryService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await categoryService.GetAllAsync();
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await categoryService.GetByIdAsync(id);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost]
    public async Task<IActionResult> Add(Category category)
    {
        var result = await categoryService.AddAsync(category);
        return result.Success ? CreatedAtAction(nameof(GetById), new { id = category.Id }, result) : BadRequest(result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, Category category)
    {
        category.Id = id;
        var result = await categoryService.UpdateAsync(category);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await categoryService.DeleteAsync(id);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
//#endif
