using MarketApp.Api.Entities;
using MarketApp.Api.Enums;
using System.Linq.Expressions;

namespace MarketApp.Api.Queries;

public static class InventoryMovementQueries
{
    public static readonly Expression<Func<InventoryMovement, int>> StockQuantity =
        m => m.IsVoided ? 0 : m.Type == MovementType.Inbound ? m.Quantity : -m.Quantity;
}
