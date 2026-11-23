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

    private const string InboundNotFoundMessage = "El ingreso no existe.";
    private const string MovementNotFoundMessage = "El movimiento no existe.";

    private const int UpdatedQuantity = 30;
    private const int UpdatedTotalCost = 22500;
    private const string UpdatedNote = "Compra corregida";

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
    public async Task Sets_the_update_date_equal_to_the_creation_date()
    {
        using var database = new TestDatabase();
        var product = await SeedProductAsync(database);

        var response = await CreateInboundAsync(database, NewRequest(product.Id));

        var movement = await FindMovementAsync(database, response.Id);
        Assert.Equal(movement.CreatedAt, movement.UpdatedAt);
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

    [Fact]
    public async Task Skips_the_excluded_movement_when_it_is_the_last_inbound()
    {
        using var database = new TestDatabase();
        var product = await SeedProductAsync(database);
        var previous = NewInbound(product.Id, 10, 5000, October(1));
        var last = NewInbound(product.Id, 20, 9000, October(15));
        await SeedMovementsAsync(database, previous, last);

        var summary = await GetStockSummaryAsync(database, product.Id, last.Id);

        Assert.Equal(10, summary.LastInboundQuantity);
        Assert.Equal(5000, summary.LastInboundTotalCost);
    }

    [Fact]
    public async Task Keeps_the_last_inbound_when_the_excluded_movement_is_an_earlier_inbound()
    {
        using var database = new TestDatabase();
        var product = await SeedProductAsync(database);
        var earlier = NewInbound(product.Id, 10, 5000, October(1));
        var last = NewInbound(product.Id, 20, 9000, October(15));
        await SeedMovementsAsync(database, earlier, last);

        var summary = await GetStockSummaryAsync(database, product.Id, earlier.Id);

        Assert.Equal(20, summary.LastInboundQuantity);
        Assert.Equal(9000, summary.LastInboundTotalCost);
    }

    [Fact]
    public async Task Skips_the_excluded_movement_when_it_wins_a_tie_on_the_movement_date()
    {
        using var database = new TestDatabase();
        var product = await SeedProductAsync(database);
        var lowerId = NewInbound(product.Id, 10, 5000, October(15));
        var higherId = NewInbound(product.Id, 20, 9000, October(15));
        await SeedMovementsAsync(database, lowerId, higherId);

        var summary = await GetStockSummaryAsync(database, product.Id, higherId.Id);

        Assert.Equal(10, summary.LastInboundQuantity);
        Assert.Equal(5000, summary.LastInboundTotalCost);
    }

    [Fact]
    public async Task Skips_the_excluded_movement_and_a_voided_inbound_before_it()
    {
        using var database = new TestDatabase();
        var product = await SeedProductAsync(database);
        var valid = NewInbound(product.Id, 10, 5000, October(1));
        var voided = NewInbound(product.Id, 30, 14000, October(8), isVoided: true);
        var excluded = NewInbound(product.Id, 20, 9000, October(15));
        await SeedMovementsAsync(database, valid, voided, excluded);

        var summary = await GetStockSummaryAsync(database, product.Id, excluded.Id);

        Assert.Equal(10, summary.LastInboundQuantity);
        Assert.Equal(5000, summary.LastInboundTotalCost);
    }

    [Fact]
    public async Task Ignores_an_excluded_id_that_does_not_exist()
    {
        using var database = new TestDatabase();
        var product = await SeedProductAsync(database);
        var last = NewInbound(product.Id, 20, 9000, October(15));
        await SeedMovementsAsync(database, last);

        var summary = await GetStockSummaryAsync(database, product.Id, last.Id + 100);

        Assert.Equal(20, summary.LastInboundQuantity);
        Assert.Equal(9000, summary.LastInboundTotalCost);
    }

    [Fact]
    public async Task Ignores_an_excluded_outbound_when_finding_the_last_inbound()
    {
        using var database = new TestDatabase();
        var product = await SeedProductAsync(database);
        var inbound = NewInbound(product.Id, 20, 9000, October(1));
        var outbound = NewOutbound(product.Id, 3, October(15));
        await SeedMovementsAsync(database, inbound, outbound);

        var summary = await GetStockSummaryAsync(database, product.Id, outbound.Id);

        Assert.Equal(20, summary.LastInboundQuantity);
        Assert.Equal(9000, summary.LastInboundTotalCost);
    }

    [Fact]
    public async Task Keeps_the_excluded_movement_in_the_stock()
    {
        using var database = new TestDatabase();
        var product = await SeedProductAsync(database);
        var first = NewInbound(product.Id, 24, 18000, October(1));
        var excluded = NewInbound(product.Id, 12, 9600, October(2));
        var outbound = NewOutbound(product.Id, 5, October(3));
        await SeedMovementsAsync(database, first, excluded, outbound);

        var summary = await GetStockSummaryAsync(database, product.Id, excluded.Id);

        Assert.Equal(31, summary.Stock);
    }

    // ---------- GetAllAsync ----------

    [Fact]
    public async Task Returns_an_empty_list_when_there_are_no_movements()
    {
        using var database = new TestDatabase();
        await SeedProductAsync(database);

        var movements = await GetAllAsync(database);

        Assert.Empty(movements);
    }

    // The movement dates go the opposite way to the creation times, so ordering by
    // the movement date would give the reverse list.
    [Fact]
    public async Task Returns_the_movements_from_the_latest_creation_time_to_the_earliest()
    {
        using var database = new TestDatabase();
        var product = await SeedProductAsync(database);
        var first = NewInbound(product.Id, 10, 5000, October(20), createdAt: OctoberAt(5, 12));
        var last = NewInbound(product.Id, 20, 9000, October(1), createdAt: OctoberAt(20, 12));
        var middle = NewInbound(product.Id, 30, 14000, October(10), createdAt: OctoberAt(10, 12));
        await SeedMovementsAsync(database, first, last, middle);

        var movements = await GetAllAsync(database);

        Assert.Equal(new[] { last.Id, middle.Id, first.Id }, movements.Select(m => m.Id));
    }

    [Fact]
    public async Task Includes_inbound_and_outbound_movements()
    {
        using var database = new TestDatabase();
        var product = await SeedProductAsync(database);
        var inbound = NewInbound(product.Id, 24, 18000, October(1));
        var outbound = NewOutbound(product.Id, 5, October(2));
        await SeedMovementsAsync(database, inbound, outbound);

        var movements = await GetAllAsync(database);

        Assert.Equal(new[] { inbound.Id, outbound.Id }, movements.Select(m => m.Id).Order());
    }

    [Fact]
    public async Task Includes_voided_movements()
    {
        using var database = new TestDatabase();
        var product = await SeedProductAsync(database);
        var valid = NewInbound(product.Id, 24, 18000, October(1));
        var voided = NewInbound(product.Id, 12, 9600, October(2), isVoided: true);
        await SeedMovementsAsync(database, valid, voided);

        var movements = await GetAllAsync(database);

        Assert.Equal(new[] { valid.Id, voided.Id }, movements.Select(m => m.Id).Order());
    }

    [Fact]
    public async Task Includes_the_movements_of_inactive_products()
    {
        using var database = new TestDatabase();
        var product = await SeedProductAsync(database, isActive: false);
        var movement = NewInbound(product.Id, 24, 18000, October(1));
        await SeedMovementsAsync(database, movement);

        var movements = await GetAllAsync(database);

        Assert.Equal(movement.Id, Assert.Single(movements).Id);
    }

    [Fact]
    public async Task Returns_every_field_of_the_movement()
    {
        using var database = new TestDatabase();
        var product = await SeedProductAsync(database);
        var movement = NewOutbound(product.Id, 3, October(8));
        movement.Note = "Venta al contado";
        movement.CreatedAt = OctoberAt(9, 15);
        await SeedMovementsAsync(database, movement);

        var response = Assert.Single(await GetAllAsync(database));

        Assert.Equal(movement.Id, response.Id);
        Assert.Equal(product.Id, response.ProductId);
        Assert.Equal("TMV00001", response.ProductCode);
        Assert.Equal("Producto TMV00001", response.ProductName);
        Assert.Equal(MovementType.Outbound, response.Type);
        Assert.Equal(3, response.Quantity);
        Assert.Equal(3750, response.Amount);
        Assert.Equal(October(8), response.MovementDate);
        Assert.Equal("Venta al contado", response.Note);
        Assert.Equal(OctoberAt(9, 15), response.CreatedAt);
    }

    [Fact]
    public async Task Returns_the_creation_time_in_utc()
    {
        using var database = new TestDatabase();
        var product = await SeedProductAsync(database);
        await SeedMovementsAsync(database, NewInbound(product.Id, 24, 18000, October(1)));

        var response = Assert.Single(await GetAllAsync(database));

        Assert.Equal(DateTimeKind.Utc, response.CreatedAt.Kind);
    }

    // ---------- UpdateInboundAsync ----------

    [Fact]
    public async Task Saves_the_new_quantity_total_cost_date_and_note()
    {
        using var database = new TestDatabase();
        var product = await SeedProductAsync(database);
        var movement = NewInbound(product.Id, 24, 18000, Today().AddDays(-10));
        await SeedMovementsAsync(database, movement);
        var newDate = Today().AddDays(-5);

        await UpdateInboundAsync(database, movement.Id, NewUpdateRequest(newDate));

        var saved = await FindMovementAsync(database, movement.Id);
        Assert.Equal(UpdatedQuantity, saved.Quantity);
        Assert.Equal(UpdatedTotalCost, saved.Amount);
        Assert.Equal(newDate, saved.MovementDate);
        Assert.Equal(UpdatedNote, saved.Note);
    }

    [Fact]
    public async Task Returns_the_updated_inbound()
    {
        using var database = new TestDatabase();
        var product = await SeedProductAsync(database);
        var movement = NewInbound(product.Id, 24, 18000, Today().AddDays(-10));
        await SeedMovementsAsync(database, movement);
        var newDate = Today().AddDays(-5);

        var response = await UpdateInboundAsync(database, movement.Id, NewUpdateRequest(newDate));

        Assert.Equal(movement.Id, response.Id);
        Assert.Equal(product.Id, response.ProductId);
        Assert.Equal(UpdatedQuantity, response.Quantity);
        Assert.Equal(UpdatedTotalCost, response.TotalCost);
        Assert.Equal(newDate, response.MovementDate);
        Assert.Equal(UpdatedNote, response.Note);
    }

    [Fact]
    public async Task Keeps_the_product_and_the_inbound_type_of_the_edited_movement()
    {
        using var database = new TestDatabase();
        var product = await SeedProductAsync(database);
        var movement = NewInbound(product.Id, 24, 18000, Today().AddDays(-10));
        await SeedMovementsAsync(database, movement);

        await UpdateInboundAsync(database, movement.Id, NewUpdateRequest(Today().AddDays(-5)));

        var saved = await FindMovementAsync(database, movement.Id);
        Assert.Equal(product.Id, saved.ProductId);
        Assert.Equal(MovementType.Inbound, saved.Type);
    }

    [Fact]
    public async Task Throws_not_found_when_the_inbound_does_not_exist()
    {
        using var database = new TestDatabase();
        var product = await SeedProductAsync(database);
        var existing = NewInbound(product.Id, 24, 18000, Today().AddDays(-10));
        await SeedMovementsAsync(database, existing);

        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => UpdateInboundAsync(database, existing.Id + 1, NewUpdateRequest(Today().AddDays(-5))));

        Assert.Equal(InboundNotFoundMessage, exception.Message);
    }

    [Fact]
    public async Task Accepts_a_new_date_exactly_one_year_before_today()
    {
        using var database = new TestDatabase();
        var product = await SeedProductAsync(database);
        var movement = NewInbound(product.Id, 24, 18000, Today().AddDays(-10));
        await SeedMovementsAsync(database, movement);
        var oneYearAgo = Today().AddYears(-1);

        await UpdateInboundAsync(database, movement.Id, NewUpdateRequest(oneYearAgo));

        var saved = await FindMovementAsync(database, movement.Id);
        Assert.Equal(oneYearAgo, saved.MovementDate);
    }

    [Fact]
    public async Task Saves_the_changes_when_an_unchanged_date_is_more_than_one_year_before_today()
    {
        using var database = new TestDatabase();
        var product = await SeedProductAsync(database);
        var twoYearsAgo = Today().AddYears(-2);
        var movement = NewInbound(product.Id, 24, 18000, twoYearsAgo);
        await SeedMovementsAsync(database, movement);

        await UpdateInboundAsync(database, movement.Id, NewUpdateRequest(twoYearsAgo));

        var saved = await FindMovementAsync(database, movement.Id);
        Assert.Equal(twoYearsAgo, saved.MovementDate);
        Assert.Equal(UpdatedQuantity, saved.Quantity);
        Assert.Equal(UpdatedTotalCost, saved.Amount);
    }

    [Fact]
    public async Task Rejects_a_new_date_more_than_one_year_before_today()
    {
        using var database = new TestDatabase();
        var product = await SeedProductAsync(database);
        var movement = NewInbound(product.Id, 24, 18000, Today().AddDays(-10));
        await SeedMovementsAsync(database, movement);
        var oneYearAgo = Today().AddYears(-1);

        var exception = await Assert.ThrowsAsync<FieldValidationException>(
            () => UpdateInboundAsync(database, movement.Id, NewUpdateRequest(oneYearAgo.AddDays(-1))));

        Assert.Equal("MovementDate", exception.Field);
        Assert.Equal($"La fecha no puede ser anterior al {oneYearAgo:dd'/'MM'/'yyyy}.", exception.Message);
    }

    // The stored date is two years old and the new one is a day later, but still
    // older than one year. The date changes, so it is validated.
    [Fact]
    public async Task Rejects_a_changed_date_more_than_one_year_before_today_when_the_stored_date_was_already_older()
    {
        using var database = new TestDatabase();
        var product = await SeedProductAsync(database);
        var twoYearsAgo = Today().AddYears(-2);
        var movement = NewInbound(product.Id, 24, 18000, twoYearsAgo);
        await SeedMovementsAsync(database, movement);
        var oneYearAgo = Today().AddYears(-1);

        var exception = await Assert.ThrowsAsync<FieldValidationException>(
            () => UpdateInboundAsync(database, movement.Id, NewUpdateRequest(twoYearsAgo.AddDays(1))));

        Assert.Equal("MovementDate", exception.Field);
        Assert.Equal($"La fecha no puede ser anterior al {oneYearAgo:dd'/'MM'/'yyyy}.", exception.Message);
    }

    [Fact]
    public async Task Accepts_today_as_the_new_date()
    {
        using var database = new TestDatabase();
        var product = await SeedProductAsync(database);
        var movement = NewInbound(product.Id, 24, 18000, Today().AddDays(-10));
        await SeedMovementsAsync(database, movement);
        var today = Today();

        await UpdateInboundAsync(database, movement.Id, NewUpdateRequest(today));

        var saved = await FindMovementAsync(database, movement.Id);
        Assert.Equal(today, saved.MovementDate);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(365)]
    public async Task Rejects_a_new_date_after_today(int daysFromToday)
    {
        using var database = new TestDatabase();
        var product = await SeedProductAsync(database);
        var movement = NewInbound(product.Id, 24, 18000, Today().AddDays(-10));
        await SeedMovementsAsync(database, movement);

        var exception = await Assert.ThrowsAsync<FieldValidationException>(
            () => UpdateInboundAsync(database, movement.Id, NewUpdateRequest(Today().AddDays(daysFromToday))));

        Assert.Equal("MovementDate", exception.Field);
        Assert.Equal(FutureDateMessage, exception.Message);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(-400)]
    public async Task Does_not_save_the_changes_when_the_new_date_is_rejected(int daysFromToday)
    {
        using var database = new TestDatabase();
        var product = await SeedProductAsync(database);
        var originalDate = Today().AddDays(-10);
        var movement = NewInbound(product.Id, 24, 18000, originalDate);
        await SeedMovementsAsync(database, movement);
        var request = NewUpdateRequest(Today().AddDays(daysFromToday));

        await Assert.ThrowsAsync<FieldValidationException>(
            () => UpdateInboundAsync(database, movement.Id, request));

        var saved = await FindMovementAsync(database, movement.Id);
        Assert.Equal(24, saved.Quantity);
        Assert.Equal(18000, saved.Amount);
        Assert.Equal(originalDate, saved.MovementDate);
        Assert.Null(saved.Note);
    }

    [Fact]
    public async Task Throws_not_found_when_the_id_belongs_to_an_outbound()
    {
        using var database = new TestDatabase();
        var product = await SeedProductAsync(database);
        var outbound = NewOutbound(product.Id, 5, Today().AddDays(-10));
        await SeedMovementsAsync(database, outbound);

        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => UpdateInboundAsync(database, outbound.Id, NewUpdateRequest(Today().AddDays(-5))));

        Assert.Equal(InboundNotFoundMessage, exception.Message);
    }

    [Fact]
    public async Task Keeps_the_creation_date_of_the_edited_inbound()
    {
        using var database = new TestDatabase();
        var product = await SeedProductAsync(database);
        var movement = NewInbound(product.Id, 24, 18000, Today().AddDays(-10), createdAt: OctoberAt(1, 12));
        await SeedMovementsAsync(database, movement);

        await UpdateInboundAsync(database, movement.Id, NewUpdateRequest(Today().AddDays(-5)));

        var saved = await FindMovementAsync(database, movement.Id);
        Assert.Equal(OctoberAt(1, 12), saved.CreatedAt);
    }

    [Fact]
    public async Task Returns_the_creation_date_of_the_edited_inbound_in_utc()
    {
        using var database = new TestDatabase();
        var product = await SeedProductAsync(database);
        var movement = NewInbound(product.Id, 24, 18000, Today().AddDays(-10));
        await SeedMovementsAsync(database, movement);

        var response = await UpdateInboundAsync(database, movement.Id, NewUpdateRequest(Today().AddDays(-5)));

        Assert.Equal(DateTimeKind.Utc, response.CreatedAt.Kind);
    }

    [Fact]
    public async Task Sets_the_update_date_of_the_edited_inbound_to_the_current_utc_time()
    {
        using var database = new TestDatabase();
        var product = await SeedProductAsync(database);
        var movement = NewInbound(product.Id, 24, 18000, Today().AddDays(-10));
        await SeedMovementsAsync(database, movement);

        var before = DateTime.UtcNow;
        await UpdateInboundAsync(database, movement.Id, NewUpdateRequest(Today().AddDays(-5)));
        var after = DateTime.UtcNow;

        var saved = await FindMovementAsync(database, movement.Id);
        Assert.InRange(saved.UpdatedAt, before, after);
    }

    [Fact]
    public async Task Saves_the_edited_note_without_outer_spaces()
    {
        using var database = new TestDatabase();
        var product = await SeedProductAsync(database);
        var movement = NewInbound(product.Id, 24, 18000, Today().AddDays(-10));
        await SeedMovementsAsync(database, movement);

        await UpdateInboundAsync(
            database, movement.Id, NewUpdateRequest(Today().AddDays(-5), note: "  Compra corregida  "));

        var saved = await FindMovementAsync(database, movement.Id);
        Assert.Equal("Compra corregida", saved.Note);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Removes_the_note_when_the_edited_note_is_missing_or_blank(string? note)
    {
        using var database = new TestDatabase();
        var product = await SeedProductAsync(database);
        var movement = NewInbound(product.Id, 24, 18000, Today().AddDays(-10));
        movement.Note = Note;
        await SeedMovementsAsync(database, movement);

        await UpdateInboundAsync(database, movement.Id, NewUpdateRequest(Today().AddDays(-5), note: note));

        var saved = await FindMovementAsync(database, movement.Id);
        Assert.Null(saved.Note);
    }

    // ---------- GetByIdAsync ----------

    [Fact]
    public async Task Returns_every_field_of_the_movement_with_the_given_id()
    {
        using var database = new TestDatabase();
        var product = await SeedProductAsync(database);
        var movement = NewOutbound(product.Id, 3, October(8));
        movement.Note = "Venta al contado";
        movement.CreatedAt = OctoberAt(9, 15);
        await SeedMovementsAsync(database, movement);

        var response = await GetByIdAsync(database, movement.Id);

        Assert.Equal(movement.Id, response.Id);
        Assert.Equal(product.Id, response.ProductId);
        Assert.Equal("TMV00001", response.ProductCode);
        Assert.Equal("Producto TMV00001", response.ProductName);
        Assert.Equal(MovementType.Outbound, response.Type);
        Assert.Equal(3, response.Quantity);
        Assert.Equal(3750, response.Amount);
        Assert.Equal(October(8), response.MovementDate);
        Assert.Equal("Venta al contado", response.Note);
        Assert.Equal(OctoberAt(9, 15), response.CreatedAt);
    }

    [Fact]
    public async Task Returns_an_inbound_movement()
    {
        using var database = new TestDatabase();
        var product = await SeedProductAsync(database);
        var inbound = NewInbound(product.Id, 24, 18000, October(1));
        await SeedMovementsAsync(database, inbound);

        var response = await GetByIdAsync(database, inbound.Id);

        Assert.Equal(inbound.Id, response.Id);
        Assert.Equal(MovementType.Inbound, response.Type);
        Assert.Equal(24, response.Quantity);
        Assert.Equal(18000, response.Amount);
    }

    [Fact]
    public async Task Returns_the_movement_with_the_given_id_among_others()
    {
        using var database = new TestDatabase();
        var product = await SeedProductAsync(database);
        var other = await SeedProductAsync(database, "TMV00002");
        var first = NewInbound(product.Id, 10, 5000, October(1));
        var wanted = NewOutbound(other.Id, 7, October(2));
        var last = NewInbound(product.Id, 30, 14000, October(3));
        await SeedMovementsAsync(database, first, wanted, last);

        var response = await GetByIdAsync(database, wanted.Id);

        Assert.Equal(wanted.Id, response.Id);
        Assert.Equal(other.Id, response.ProductId);
        Assert.Equal(7, response.Quantity);
    }

    [Theory]
    [InlineData(MovementType.Inbound)]
    [InlineData(MovementType.Outbound)]
    public async Task Returns_a_voided_movement(MovementType type)
    {
        using var database = new TestDatabase();
        var product = await SeedProductAsync(database);
        var movement = type == MovementType.Inbound
            ? NewInbound(product.Id, 24, 18000, October(1), isVoided: true)
            : NewOutbound(product.Id, 5, October(1), isVoided: true);
        await SeedMovementsAsync(database, movement);

        var response = await GetByIdAsync(database, movement.Id);

        Assert.Equal(movement.Id, response.Id);
        Assert.Equal(type, response.Type);
    }

    [Fact]
    public async Task Returns_a_movement_of_an_inactive_product()
    {
        using var database = new TestDatabase();
        var product = await SeedProductAsync(database, isActive: false);
        var movement = NewInbound(product.Id, 24, 18000, October(1));
        await SeedMovementsAsync(database, movement);

        var response = await GetByIdAsync(database, movement.Id);

        Assert.Equal(movement.Id, response.Id);
        Assert.Equal(product.Id, response.ProductId);
    }

    [Fact]
    public async Task Returns_the_creation_time_of_the_movement_in_utc()
    {
        using var database = new TestDatabase();
        var product = await SeedProductAsync(database);
        var movement = NewInbound(product.Id, 24, 18000, October(1));
        await SeedMovementsAsync(database, movement);

        var response = await GetByIdAsync(database, movement.Id);

        Assert.Equal(DateTimeKind.Utc, response.CreatedAt.Kind);
    }

    [Fact]
    public async Task Throws_not_found_when_the_movement_does_not_exist()
    {
        using var database = new TestDatabase();
        var product = await SeedProductAsync(database);
        var existing = NewInbound(product.Id, 24, 18000, October(1));
        await SeedMovementsAsync(database, existing);

        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => GetByIdAsync(database, existing.Id + 1));

        Assert.Equal(MovementNotFoundMessage, exception.Message);
    }

    private static async Task<InventoryMovementResponse> GetByIdAsync(TestDatabase database, int id)
    {
        using var context = database.CreateContext();
        var service = new InventoryMovementService(context, NullLogger<InventoryMovementService>.Instance);
        return await service.GetByIdAsync(id);
    }

    private static UpdateInboundMovementRequest NewUpdateRequest(DateOnly movementDate, string? note = UpdatedNote) =>
        new()
        {
            Quantity = UpdatedQuantity,
            TotalCost = UpdatedTotalCost,
            MovementDate = movementDate,
            Note = note,
        };

    private static async Task<InboundMovementResponse> UpdateInboundAsync(
        TestDatabase database, int id, UpdateInboundMovementRequest request)
    {
        using var context = database.CreateContext();
        var service = new InventoryMovementService(context, NullLogger<InventoryMovementService>.Instance);
        return await service.UpdateInboundAsync(id, request);
    }

    private static DateTime OctoberAt(int day, int hour) => new(2026, 10, day, hour, 0, 0, DateTimeKind.Utc);

    private static async Task<List<InventoryMovementResponse>> GetAllAsync(TestDatabase database)
    {
        using var context = database.CreateContext();
        var service = new InventoryMovementService(context, NullLogger<InventoryMovementService>.Instance);
        return await service.GetAllAsync();
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

    private static async Task<StockSummaryResponse> GetStockSummaryAsync(
        TestDatabase database, int productId, int? excludedMovementId = null)
    {
        using var context = database.CreateContext();
        var service = new InventoryMovementService(context, NullLogger<InventoryMovementService>.Instance);
        return await service.GetStockSummaryAsync(productId, excludedMovementId);
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
