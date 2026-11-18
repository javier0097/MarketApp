using MarketApp.Api.Dtos.Requests;
using MarketApp.Api.Dtos.Responses;

namespace MarketApp.Api.Services.Interfaces;

public interface IInventoryMovementService
{
    Task<InboundMovementResponse> CreateInboundAsync(CreateInboundMovementRequest request);

    Task<InboundMovementResponse> UpdateInboundAsync(int id, UpdateInboundMovementRequest request);

    Task<List<InventoryMovementResponse>> GetAllAsync();

    Task<InventoryMovementResponse> GetByIdAsync(int id);

    Task<StockSummaryResponse> GetStockSummaryAsync(int productId, int? excludedMovementId = null);
}
