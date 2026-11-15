using MarketApp.Api.Enums;

namespace MarketApp.Api.Dtos.Responses;

public record InventoryMovementResponse(
    int Id,
    int ProductId,
    string ProductCode,
    string ProductName,
    MovementType Type,
    int Quantity,
    int Amount,
    DateOnly MovementDate,
    string? Note,
    DateTime CreatedAt);
