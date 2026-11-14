using MarketApp.Api.Enums;

namespace MarketApp.Api.Entities;

public class InventoryMovement
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public MovementType Type { get; set; }
    public int Quantity { get; set; }
    public int Amount { get; set; }
    public DateOnly MovementDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? CreatedById { get; set; }
    public string? Note { get; set; }
    public bool IsVoided { get; set; }
    public string? VoidReason { get; set; }
}
