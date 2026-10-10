using MarketApp.Api.Data;
using MarketApp.Api.Dtos.Responses;
using Microsoft.AspNetCore.Mvc;

namespace MarketApp.Api.Controllers;

[ApiController]
[Route("api/health")]
public class HealthController(MarketAppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        if (!await db.Database.CanConnectAsync(cancellationToken))
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new HealthResponse("database_unavailable"));
        }

        return Ok(new HealthResponse("ok"));
    }
}
