namespace MarketApp.Api.Dtos.Responses;

public record InboundMovementResponse(int Id, int ProductId, int Quantity, int TotalCost, DateOnly MovementDate, string? Note, DateTime CreatedAt);
