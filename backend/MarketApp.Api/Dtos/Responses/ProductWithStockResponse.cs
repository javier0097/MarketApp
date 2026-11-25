namespace MarketApp.Api.Dtos.Responses;

public record ProductWithStockResponse(int Id, string Code, string Name, int SalePrice, int Stock, DateTime UpdatedAt);
