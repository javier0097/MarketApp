using MarketApp.Api.Dtos.Requests;
using MarketApp.Api.Dtos.Responses;

namespace MarketApp.Api.Services.Interfaces;

public interface IInventoryMovementService
{
    Task<InboundMovementResponse> CreateInboundAsync(CreateInboundMovementRequest request);

    Task<List<InventoryMovementResponse>> GetAllAsync();

    Task<StockSummaryResponse> GetStockSummaryAsync(int productId);
}
