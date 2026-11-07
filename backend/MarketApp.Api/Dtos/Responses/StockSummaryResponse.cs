namespace MarketApp.Api.Dtos.Responses;

public record StockSummaryResponse(int Stock, int? LastInboundQuantity, int? LastInboundTotalCost);
