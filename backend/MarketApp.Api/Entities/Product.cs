namespace MarketApp.Api.Entities;

public class Product
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Code { get; set; }
    public int SalePrice { get; set; }
    public int MinStock { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int? CreatedById { get; set; }
    public int? UpdatedById { get; set; }
}
