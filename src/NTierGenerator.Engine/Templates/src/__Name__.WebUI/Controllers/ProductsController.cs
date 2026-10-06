//#if Sample
using __Name__.Business.Abstract;
using Microsoft.AspNetCore.Mvc;

namespace __Name__.WebUI.Controllers;

public class ProductsController(IProductService productService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var result = await productService.GetProductDetailsAsync();
        return View(result.Data ?? []);
    }
}
//#endif
