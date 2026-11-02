using MarketApp.Api.Dtos.Requests;
using MarketApp.Api.Dtos.Responses;
using MarketApp.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MarketApp.Api.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController(IProductService productService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetActive()
    {
        var products = await productService.GetActiveAsync();
        return Ok(products);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var product = await productService.GetByIdAsync(id);
        return Ok(product);
    }

    [HttpPost]
    public async Task<IActionResult> Create(SaveProductRequest request)
    {
        var product = await productService.CreateAsync(request);
        return StatusCode(StatusCodes.Status201Created, product);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, SaveProductRequest request)
    {
        var product = await productService.UpdateAsync(id, request);
        return Ok(product);
    }

    [HttpGet("next-internal-code")]
    public async Task<IActionResult> GetNextInternalCode()
    {
        var code = await productService.GetNextInternalCodeAsync();
        return Ok(new NextInternalCodeResponse(code));
    }
}
