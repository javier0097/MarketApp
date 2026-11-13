using MarketApp.Api.Dtos.Requests;
using MarketApp.Api.Dtos.Responses;
using MarketApp.Api.Entities;
using MarketApp.Api.Enums;
using MarketApp.Api.Exceptions;
using MarketApp.Api.Services;
using MarketApp.Api.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace MarketApp.Api.Tests.ServiceTests;

public class InventoryMovementServiceTests
{
    private const string ProductNotFoundMessage = "El producto no existe.";
    private const string FutureDateMessage = "La fecha no puede ser posterior a hoy.";

    // 24 units for Bs 180,00 (amounts are in cents).
    private const int Quantity = 24;
    private const int TotalCost = 18000;
    private const string Note = "Compra al proveedor";

    // ---------- CreateInboundAsync ----------

    [Fact]
    public async Task Returns_the_created_movement()
    {
        using var database = new TestDatabase();
        var product = await SeedProductAsync(database);
        var movementDate = Today().AddDays(-1);

        var response = await CreateInboundAsync(database, NewRequest(product.Id, movementDate));

        Assert.True(response.Id > 0);
        Assert.Equal(product.Id, response.ProductId);
        Assert.Equal(Quantity, response.Quantity);
        Assert.Equal(TotalCost, response.TotalCost);
        Assert.Equal(movementDate, response.MovementDate);
        Assert.Equal(Note, response.Note);
    }

    [Fact]
    public async Task Saves_the_product_the_quantity_and_the_total_cost_as_the_amount()
    {
        using var database = new TestDatabase();
        var product = await SeedProductAsync(database);

        var response = await CreateInboundAsync(database, NewRequest(product.Id));

        var movement = await FindMovementAsync(database, response.Id);
        Assert.Equal(product.Id, movement.ProductId);
        Assert.Equal(Quantity, movement.Quantity);
        Assert.Equal(TotalCost, movement.Amount);
    }

    [Fact]
    public async Task Saves_the_movement_as_inbound()
    {
        using var database = new TestDatabase();
        var product = await SeedProductAsync(database);

        var response = await CreateInboundAsync(database, NewRequest(product.Id));

        var movement = await FindMovementAsync(database, response.Id);
        Assert.Equal(MovementType.Inbound, movement.Type);
    }

    [Fact]
    public async Task Saves_the_movement_as_not_voided_and_without_a_void_reason()
    {
        using var database = new TestDatabase();
        var product = await SeedProductAsync(database);

        var response = await CreateInboundAsync(database, NewRequest(product.Id));

        var movement = await FindMovementAsync(database, response.Id);
        Assert.False(movement.IsVoided);
        Assert.Null(movement.VoidReason);
    }

    [Fact]
    public async Task Sets_the_creation_date_to_the_current_utc_time()
    {
        using var database = new TestDatabase();
        var product = await SeedProductAsync(database);

        var before = DateTime.UtcNow;
        var response = await CreateInboundAsync(database, NewRequest(product.Id));
        var after = DateTime.UtcNow;

        var movement = await FindMovementAsync(database, response.Id);
        Assert.InRange(movement.CreatedAt, before, after);
    }

    [Fact]
    public async Task Returns_the_creation_date_that_was_saved()
    {
        using var database = new TestDatabase();
        var product = await SeedProductAsync(database);

        var response = await CreateInboundAsync(database, NewRequest(product.Id));

        var movement = await FindMovementAsync(database, response.Id);
        Assert.Equal(movement.CreatedAt, response.CreatedAt);
    }

    [Fact]
    public async Task Returns_the_creation_date_in_utc()
    {
        using var database = new TestDatabase();
        var product = await SeedProductAsync(database);

        var response = await CreateInboundAsync(database, NewRequest(product.Id));

        Assert.Equal(DateTimeKind.Utc, response.CreatedAt.Kind);
    }

    [Fact]
    public async Task Accepts_today_as_the_movement_date()
    {
        using var database = new TestDatabase();
        var product = await SeedProductAsync(database);
        var today = Today();

        var response = await CreateInboundAsync(database, NewRequest(product.Id, today));

        var movement = await FindMovementAsync(database, response.Id);
        Assert.Equal(today, movement.MovementDate);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-365)]
    public async Task Accepts_a_movement_date_before_today(int daysFromToday)
    {
        using var database = new TestDatabase();
        var product = await SeedProductAsync(database);
        var movementDate = Today().AddDays(daysFromToday);

        var response = await CreateInboundAsync(database, NewRequest(product.Id, movementDate));

        var movement = await FindMovementAsync(database, response.Id);
        Assert.Equal(movementDate, movement.MovementDate);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(365)]
    public async Task Rejects_a_movement_date_after_today(int daysFromToday)
    {
        using var database = new TestDatabase();
        var product = await SeedProductAsync(database);

        var exception = await Assert.ThrowsAsync<FieldValidationException>(
            () => CreateInboundAsync(database, NewRequest(product.Id, Today().AddDays(daysFromToday))));

        Assert.Equal("MovementDate", exception.Field);
        Assert.Equal(FutureDateMessage, exception.Message);
    }

    [Fact]
    public async Task Accepts_a_movement_date_exactly_one_year_before_today()
    {
        using var database = new TestDatabase();
        var product = await SeedProductAsync(database);
        var movementDate = Today().AddYears(-1);

        var response = await CreateInboundAsync(database, NewRequest(product.Id, movementDate));

        var movement = await FindMovementAsync(database, response.Id);
        Assert.Equal(movementDate, movement.MovementDate);
    }

    [Fact]
    public async Task Rejects_a_movement_date_more_than_one_year_before_today()
    {
        using var database = new TestDatabase();
        var product = await SeedProductAsync(database);
        var oneYearAgo = Today().AddYears(-1);

        var exception = await Assert.ThrowsAsync<FieldValidationException>(
            () => CreateInboundAsync(database, NewRequest(product.Id, oneYearAgo.AddDays(-1))));

        Assert.Equal("MovementDate", exception.Field);
        Assert.Equal($"La fecha no puede ser anterior al {oneYearAgo:dd'/'MM'/'yyyy}.", exception.Message);
    }

    [Fact]
    public async Task Rejects_a_product_that_does_not_exist()
    {
        using var database = new TestDatabase();
        var existing = await SeedProductAsync(database);

        var exception = await Assert.ThrowsAsync<FieldValidationException>(
            () => CreateInboundAsync(database, NewRequest(existing.Id + 1)));

        Assert.Equal("ProductId", exception.Field);
        Assert.Equal(ProductNotFoundMessage, exception.Message);
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(0, 1)]
    public async Task Does_not_save_a_movement_that_fails_validation(int productIdOffset, int daysFromToday)
    {
        using var database = new TestDatabase();
        var existing = await SeedProductAsync(database);
        var request = NewRequest(existing.Id + productIdOffset, Today().AddDays(daysFromToday));

        await Assert.ThrowsAsync<FieldValidationException>(() => CreateInboundAsync(database, request));

        using var verification = database.CreateContext();
        var count = await verification.Set<InventoryMovement>().CountAsync(TestContext.Current.CancellationToken);
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task Saves_the_note_without_outer_spaces()
    {
        using var database = new TestDatabase();
        var product = await SeedProductAsync(database);

        var response = await CreateInboundAsync(database, NewRequest(product.Id, note: "  Compra al proveedor  "));

        var movement = await FindMovementAsync(database, response.Id);
        Assert.Equal("Compra al proveedor", movement.Note);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Saves_no_note_when_the_note_is_missing_or_blank(string? note)
    {
        using var database = new TestDatabase();
        var product = await SeedProductAsync(database);

        var response = await CreateInboundAsync(database, NewRequest(product.Id, note: note));

        var movement = await FindMovementAsync(database, response.Id);
        Assert.Null(movement.Note);
    }

    // ---------- GetStockSummaryAsync ----------

    [Fact]
    public async Task Returns_a_stock_of_zero_when_the_product_has_no_movements()
    {
        using var database = new TestDatabase();
        var product = await SeedProductAsync(database);

        var summary = await GetStockSummaryAsync(database, product.Id);

        Assert.Equal(0, summary.Stock);
    }

    [Fact]
    public async Task Returns_no_last_inbound_when_the_product_has_no_movements()
    {
        using var database = new TestDatabase();
        var product = await SeedProductAsync(database);

        var summary = await GetStockSummaryAsync(database, product.Id);

        Assert.Null(summary.LastInboundQuantity);
        Assert.Null(summary.LastInboundTotalCost);
    }
    [Fact]
    public async Task Adds_the_inbound_quantities_and_subtracts_the_outbound_ones()
    {
        using var database = new TestDatabase();
        var product = await SeedProductAsync(database);
        await SeedMovementsAsync(
            database,
            NewInbound(product.Id, 24, 18000, October(1)),
            NewInbound(product.Id, 12, 9600, October(2)),
            NewOutbound(product.Id, 5, October(3)));

        var summary = await GetStockSummaryAsync(database, product.Id);

        Assert.Equal(31, summary.Stock);
    }

    [Fact]
    public async Task Leaves_voided_movements_out_of_the_stock()
    {
        using var database = new TestDatabase();
        var product = await SeedProductAsync(database);
        await SeedMovementsAsync(
            database,
            NewInbound(product.Id, 24, 18000, October(1)),
            NewInbound(product.Id, 12, 9600, October(2), isVoided: true),
            NewOutbound(product.Id, 5, October(3), isVoided: true),
            NewOutbound(product.Id, 3, October(4)));

        var summary = await GetStockSummaryAsync(database, product.Id);

        Assert.Equal(21, summary.Stock);
    }

    [Fact]
    public async Task Leaves_the_movements_of_other_products_out_of_the_stock()
    {
        using var database = new TestDatabase();
        var product = await SeedProductAsync(database);
        var other = await SeedProductAsync(database, "TMV00002");
        await SeedMovementsAsync(
            database,
            NewInbound(product.Id, 24, 18000, October(1)),
            NewInbound(other.Id, 100, 50000, October(2)),
            NewOutbound(other.Id, 7, October(3)));

        var summary = await GetStockSummaryAsync(database, product.Id);

        Assert.Equal(24, summary.Stock);
    }

    [Fact]
    public async Task Returns_the_inbound_with_the_latest_movement_date()
    {
        using var database = new TestDatabase();
        var product = await SeedProductAsync(database);
        await SeedMovementsAsync(
            database,
            NewInbound(product.Id, 10, 5000, October(1)),
            NewInbound(product.Id, 20, 9000, October(15)),
            NewInbound(product.Id, 30, 14000, October(8)));

        var summary = await GetStockSummaryAsync(database, product.Id);

        Assert.Equal(20, summary.LastInboundQuantity);
        Assert.Equal(9000, summary.LastInboundTotalCost);
    }

    [Fact]
    public async Task Breaks_a_tie_on_the_movement_date_with_the_highest_id()
    {
        using var database = new TestDatabase();
        var product = await SeedProductAsync(database);
        await SeedMovementsAsync(
            database,
            NewInbound(product.Id, 10, 5000, October(15), createdAt: new DateTime(2026, 10, 15, 18, 0, 0, DateTimeKind.Utc)),
            NewInbound(product.Id, 20, 9000, October(15), createdAt: new DateTime(2026, 10, 15, 9, 0, 0, DateTimeKind.Utc)));

        var summary = await GetStockSummaryAsync(database, product.Id);

        Assert.Equal(20, summary.LastInboundQuantity);
        Assert.Equal(9000, summary.LastInboundTotalCost);
    }

    [Fact]
    public async Task Skips_a_voided_inbound_and_returns_the_previous_one()
    {
        using var database = new TestDatabase();
        var product = await SeedProductAsync(database);
        await SeedMovementsAsync(
            database,
            NewInbound(product.Id, 10, 5000, October(1)),
            NewInbound(product.Id, 20, 9000, October(15), isVoided: true));

        var summary = await GetStockSummaryAsync(database, product.Id);

        Assert.Equal(10, summary.LastInboundQuantity);
        Assert.Equal(5000, summary.LastInboundTotalCost);
    }

    [Fact]
    public async Task Returns_no_last_inbound_when_every_inbound_is_voided()
    {
        using var database = new TestDatabase();
        var product = await SeedProductAsync(database);
        await SeedMovementsAsync(database, NewInbound(product.Id, 10, 5000, October(1), isVoided: true));

        var summary = await GetStockSummaryAsync(database, product.Id);

        Assert.Null(summary.LastInboundQuantity);
        Assert.Null(summary.LastInboundTotalCost);
    }

    [Fact]
    public async Task Ignores_a_later_outbound_when_finding_the_last_inbound()
    {
        using var database = new TestDatabase();
        var product = await SeedProductAsync(database);
        await SeedMovementsAsync(
            database,
            NewInbound(product.Id, 10, 5000, October(1)),
            NewOutbound(product.Id, 3, October(15)));

        var summary = await GetStockSummaryAsync(database, product.Id);

        Assert.Equal(10, summary.LastInboundQuantity);
        Assert.Equal(5000, summary.LastInboundTotalCost);
    }

    [Fact]
    public async Task Ignores_a_later_inbound_of_another_product_when_finding_the_last_inbound()
    {
        using var database = new TestDatabase();
        var product = await SeedProductAsync(database);
        var other = await SeedProductAsync(database, "TMV00002");
        await SeedMovementsAsync(
            database,
            NewInbound(product.Id, 10, 5000, October(1)),
            NewInbound(other.Id, 20, 9000, October(15)));

        var summary = await GetStockSummaryAsync(database, product.Id);

        Assert.Equal(10, summary.LastInboundQuantity);
        Assert.Equal(5000, summary.LastInboundTotalCost);
    }

    [Fact]
    public async Task Throws_not_found_when_the_product_does_not_exist()
    {
        using var database = new TestDatabase();
        var existing = await SeedProductAsync(database);

        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => GetStockSummaryAsync(database, existing.Id + 1));

        Assert.Equal(ProductNotFoundMessage, exception.Message);
    }

    private static DateOnly Today() => DateOnly.FromDateTime(DateTime.Now);

    private static DateOnly October(int day) => new(2026, 10, day);

    private static InventoryMovement NewInbound(
        int productId,
        int quantity,
        int totalCost,
        DateOnly movementDate,
        bool isVoided = false,
        DateTime? createdAt = null) =>
        NewMovement(productId, MovementType.Inbound, quantity, totalCost, movementDate, isVoided, createdAt);

    private static InventoryMovement NewOutbound(
        int productId, int quantity, DateOnly movementDate, bool isVoided = false) =>
        NewMovement(productId, MovementType.Outbound, quantity, quantity * 1250, movementDate, isVoided, null);

    private static InventoryMovement NewMovement(
        int productId,
        MovementType type,
        int quantity,
        int amount,
        DateOnly movementDate,
        bool isVoided,
        DateTime? createdAt) =>
        new()
        {
            ProductId = productId,
            Type = type,
            Quantity = quantity,
            Amount = amount,
            MovementDate = movementDate,
            CreatedAt = createdAt ?? new DateTime(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc),
            IsVoided = isVoided,
            VoidReason = isVoided ? "Registrado por error" : null,
        };

    // One save per movement, so the ids follow the order of the arguments.
    private static async Task SeedMovementsAsync(TestDatabase database, params InventoryMovement[] movements)
    {
        using var context = database.CreateContext();
        foreach (var movement in movements)
        {
            context.Set<InventoryMovement>().Add(movement);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
    }

    private static async Task<StockSummaryResponse> GetStockSummaryAsync(TestDatabase database, int productId)
    {
        using var context = database.CreateContext();
        var service = new InventoryMovementService(context, NullLogger<InventoryMovementService>.Instance);
        return await service.GetStockSummaryAsync(productId);
    }

    private static CreateInboundMovementRequest NewRequest(
        int productId, DateOnly? movementDate = null, string? note = Note) =>
        new()
        {
            ProductId = productId,
            Quantity = Quantity,
            TotalCost = TotalCost,
            MovementDate = movementDate ?? Today(),
            Note = note,
        };

    private static async Task<Product> SeedProductAsync(
        TestDatabase database, string code = "TMV00001", bool isActive = true)
    {
        using var context = database.CreateContext();
        var product = new Product { Code = code, Name = $"Producto {code}", IsActive = isActive };
        context.Products.Add(product);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return product;
    }

    private static async Task<InboundMovementResponse> CreateInboundAsync(
        TestDatabase database, CreateInboundMovementRequest request)
    {
        using var context = database.CreateContext();
        var service = new InventoryMovementService(context, NullLogger<InventoryMovementService>.Instance);
        return await service.CreateInboundAsync(request);
    }

    private static async Task<InventoryMovement> FindMovementAsync(TestDatabase database, int id)
    {
        using var context = database.CreateContext();
        return await context.Set<InventoryMovement>().SingleAsync(
            movement => movement.Id == id,
            TestContext.Current.CancellationToken);
    }
}
