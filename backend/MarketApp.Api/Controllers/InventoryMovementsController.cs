using MarketApp.Api.Dtos.Requests;
using MarketApp.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MarketApp.Api.Controllers;

[ApiController]
[Route("api/inventory-movements")]
public class InventoryMovementsController(IInventoryMovementService inventoryMovementService) : ControllerBase
{
    [HttpPost("inbound")]
    public async Task<IActionResult> CreateInbound(CreateInboundMovementRequest request)
    {
        var movement = await inventoryMovementService.CreateInboundAsync(request);
        return StatusCode(StatusCodes.Status201Created, movement);
    }

    [HttpGet("stock-summary")]
    public async Task<IActionResult> GetStockSummary(int productId)
    {
        var summary = await inventoryMovementService.GetStockSummaryAsync(productId);
        return Ok(summary);
    }
}
