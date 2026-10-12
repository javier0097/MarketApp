using MarketApp.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarketApp.Api.Data;

public class MarketAppDbContext(DbContextOptions<MarketAppDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(product => product.HasIndex(p => p.Code).IsUnique());

        modelBuilder.Entity<InventoryMovement>(movement =>
        {
            movement.HasOne<Product>().WithMany().HasForeignKey(m => m.ProductId).OnDelete(DeleteBehavior.Restrict);
            movement.ToTable(t =>
            {
                t.HasCheckConstraint("CK_InventoryMovement_Quantity_Positive", "\"Quantity\" > 0");
                t.HasCheckConstraint("CK_InventoryMovement_Amount_NonNegative", "\"Amount\" >= 0");
            });
        });
    }
}
