using MarketApp.Api.Data;
using MarketApp.Api.Dtos.Requests;
using MarketApp.Api.Dtos.Responses;
using MarketApp.Api.Entities;
using MarketApp.Api.Enums;
using MarketApp.Api.Exceptions;
using MarketApp.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MarketApp.Api.Services;

public class InventoryMovementService(MarketAppDbContext db, ILogger<InventoryMovementService> logger) : IInventoryMovementService
{
    public async Task<InboundMovementResponse> CreateInboundAsync(CreateInboundMovementRequest request)
    {
        var productId = request.ProductId!.Value;
        if (!await db.Products.AnyAsync(p => p.Id == productId))
        {
            throw new FieldValidationException(nameof(CreateInboundMovementRequest.ProductId), "El producto no existe.");
        }

        var movementDate = request.MovementDate!.Value;
        if (movementDate > DateOnly.FromDateTime(DateTime.Now))
        {
            throw new FieldValidationException(nameof(CreateInboundMovementRequest.MovementDate), "La fecha no puede ser posterior a hoy.");
        }

        var movement = new InventoryMovement
        {
            ProductId = productId,
            Type = MovementType.Inbound,
            Quantity = request.Quantity!.Value,
            Amount = request.TotalCost!.Value,
            MovementDate = movementDate,
            CreatedAt = DateTime.UtcNow,
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
        };

        db.InventoryMovements.Add(movement);
        await db.SaveChangesAsync();
        logger.LogInformation(
            "Inbound movement {MovementId} created: product {ProductId} {Quantity} {TotalCost} {MovementDate} {Note}",
            movement.Id, movement.ProductId, movement.Quantity, movement.Amount, movement.MovementDate, movement.Note);

        return new InboundMovementResponse(
            movement.Id, movement.ProductId, movement.Quantity, movement.Amount, movement.MovementDate, movement.Note, movement.CreatedAt);
    }
}
