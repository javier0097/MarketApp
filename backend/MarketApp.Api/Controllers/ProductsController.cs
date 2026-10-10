using MarketApp.Api.Dtos;
using MarketApp.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MarketApp.Api.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController(IProductService productService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateProductRequest request)
    {
        var product = await productService.CreateAsync(request);
        return StatusCode(StatusCodes.Status201Created, product);
    }

    [HttpGet("next-internal-code")]
    public async Task<IActionResult> GetNextInternalCode()
    {
        var code = await productService.GetNextInternalCodeAsync();
        return Ok(new NextInternalCodeResponse(code));
    }
}
