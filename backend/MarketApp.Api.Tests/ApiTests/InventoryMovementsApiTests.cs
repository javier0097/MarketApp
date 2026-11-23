using MarketApp.Api.Data;
using MarketApp.Api.Entities;
using MarketApp.Api.Enums;
using MarketApp.Api.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace MarketApp.Api.Tests.ApiTests;

public class InventoryMovementsApiTests
{
    private const string MovementsUrl = "/api/inventory-movements";
    private const string InboundUrl = "/api/inventory-movements/inbound";
    private const string StockSummaryUrl = "/api/inventory-movements/stock-summary";
    private const string ProductsUrl = "/api/products";

    private const string ProductRequiredMessage =
        "Falta el producto. Búsquelo por nombre o código, o escanee su código de barras.";
    private const string QuantityRequiredMessage = "Ingrese la cantidad de unidades del lote.";
    private const string QuantityRangeMessage = "La cantidad debe estar entre 1 y 99.999.";
    private const string TotalCostRequiredMessage = "Ingrese el costo total del lote.";
    private const string TotalCostRangeMessage = "El costo total debe estar entre Bs 0,00 y Bs 99.999,99.";
    private const string MovementDateRequiredMessage = "Ingrese la fecha del movimiento.";
    private const string NoteLengthMessage = "La nota no puede tener más de 500 caracteres.";
    private const string ProductNotFoundMessage = "El producto no existe.";
    private const string FutureDateMessage = "La fecha no puede ser posterior a hoy.";
    private const string InboundNotFoundMessage = "El ingreso no existe.";
    private const string MovementNotFoundMessage = "El movimiento no existe.";

    private const int Quantity = 24;
    private const int TotalCost = 18000;
    private static readonly DateOnly Yesterday = DateOnly.FromDateTime(DateTime.Now).AddDays(-1);
    private static readonly string MovementDate = Yesterday.ToString("yyyy-MM-dd");
    private const string Note = "Compra al proveedor";

    // The edit changes the inbound to 30 units for Bs 225,00.
    private const int UpdatedQuantity = 30;
    private const int UpdatedTotalCost = 22500;
    private const string UpdatedNote = "Compra corregida";

    // ---------- POST /api/inventory-movements/inbound ----------

    [Fact]
    public async Task Create_returns_201_with_the_created_movement()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var productId = await CreateProductAsync(client);

        var before = DateTime.UtcNow;
        using var response = await PostInboundAsync(client, productId);
        var after = DateTime.UtcNow;
        var body = await response.Content.ReadFromJsonAsync<InboundBody>(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(body);
        Assert.True(body.Id > 0);
        Assert.Equal(productId, body.ProductId);
        Assert.Equal(Quantity, body.Quantity);
        Assert.Equal(TotalCost, body.TotalCost);
        Assert.Equal(Yesterday, body.MovementDate);
        Assert.Equal(Note, body.Note);
        Assert.InRange(body.CreatedAt, before, after);
    }

    [Fact]
    public async Task Create_sends_the_movement_date_as_year_month_day()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var productId = await CreateProductAsync(client);

        using var response = await PostInboundAsync(client, productId);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);

        Assert.Equal(MovementDate, body.GetProperty("movementDate").GetString());
    }

    [Fact]
    public async Task Create_sends_the_creation_date_in_utc()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var productId = await CreateProductAsync(client);

        using var response = await PostInboundAsync(client, productId);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);

        var createdAt = body.GetProperty("createdAt").GetString();
        Assert.NotNull(createdAt);
        Assert.EndsWith("Z", createdAt);
    }

    [Theory]
    [InlineData("""{"productId":1,"quantity":24,"totalCost":18000,"movementDate":"{date}"}""")]
    [InlineData("""{"productId":1,"quantity":24,"totalCost":18000,"movementDate":"{date}","note":""}""")]
    public async Task Create_accepts_a_request_without_a_note_or_with_an_empty_note(string json)
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        await CreateProductAsync(client);

        using var response = await PostRawJsonAsync(client, json.Replace("{date}", MovementDate));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Theory]
    [InlineData("""{"quantity":24,"totalCost":18000,"movementDate":"2026-11-01"}""", "productId", ProductRequiredMessage)]
    [InlineData("""{"productId":null,"quantity":24,"totalCost":18000,"movementDate":"2026-11-01"}""", "productId", ProductRequiredMessage)]
    [InlineData("""{"productId":1,"totalCost":18000,"movementDate":"2026-11-01"}""", "quantity", QuantityRequiredMessage)]
    [InlineData("""{"productId":1,"quantity":null,"totalCost":18000,"movementDate":"2026-11-01"}""", "quantity", QuantityRequiredMessage)]
    [InlineData("""{"productId":1,"quantity":24,"movementDate":"2026-11-01"}""", "totalCost", TotalCostRequiredMessage)]
    [InlineData("""{"productId":1,"quantity":24,"totalCost":null,"movementDate":"2026-11-01"}""", "totalCost", TotalCostRequiredMessage)]
    [InlineData("""{"productId":1,"quantity":24,"totalCost":18000}""", "movementDate", MovementDateRequiredMessage)]
    [InlineData("""{"productId":1,"quantity":24,"totalCost":18000,"movementDate":null}""", "movementDate", MovementDateRequiredMessage)]
    public async Task Create_returns_400_when_a_required_field_is_missing(string json, string field, string message)
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        await CreateProductAsync(client);

        using var response = await PostRawJsonAsync(client, json);
        var body = await ReadValidationProblemAsync(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var (key, messages) = Assert.Single(body.Errors);
        Assert.Equal(field, key);
        Assert.Equal([message], messages);
    }

    [Fact]
    public async Task Create_returns_every_error_when_several_fields_fail()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        using var response = await PostRawJsonAsync(client, "{}");
        var body = await ReadValidationProblemAsync(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(4, body.Errors.Count);
        Assert.Equal([ProductRequiredMessage], body.Errors["productId"]);
        Assert.Equal([QuantityRequiredMessage], body.Errors["quantity"]);
        Assert.Equal([TotalCostRequiredMessage], body.Errors["totalCost"]);
        Assert.Equal([MovementDateRequiredMessage], body.Errors["movementDate"]);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(99999)]
    public async Task Create_accepts_a_quantity_at_the_limits_of_the_range(int quantity)
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var productId = await CreateProductAsync(client);

        using var response = await PostInboundAsync(client, productId, quantity: quantity);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(100000)]
    public async Task Create_returns_400_when_the_quantity_is_out_of_range(int quantity)
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var productId = await CreateProductAsync(client);

        using var response = await PostInboundAsync(client, productId, quantity: quantity);
        var body = await ReadValidationProblemAsync(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var (key, messages) = Assert.Single(body.Errors);
        Assert.Equal("quantity", key);
        Assert.Equal([QuantityRangeMessage], messages);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(9999999)]
    public async Task Create_accepts_a_total_cost_at_the_limits_of_the_range(int totalCost)
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var productId = await CreateProductAsync(client);

        using var response = await PostInboundAsync(client, productId, totalCost: totalCost);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(10000000)]
    public async Task Create_returns_400_when_the_total_cost_is_out_of_range(int totalCost)
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var productId = await CreateProductAsync(client);

        using var response = await PostInboundAsync(client, productId, totalCost: totalCost);
        var body = await ReadValidationProblemAsync(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var (key, messages) = Assert.Single(body.Errors);
        Assert.Equal("totalCost", key);
        Assert.Equal([TotalCostRangeMessage], messages);
    }

    [Fact]
    public async Task Create_accepts_a_note_of_500_characters()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var productId = await CreateProductAsync(client);

        using var response = await PostInboundAsync(client, productId, note: new string('A', 500));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Create_returns_400_when_the_note_is_longer_than_500_characters()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var productId = await CreateProductAsync(client);

        using var response = await PostInboundAsync(client, productId, note: new string('A', 501));
        var body = await ReadValidationProblemAsync(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var (key, messages) = Assert.Single(body.Errors);
        Assert.Equal("note", key);
        Assert.Equal([NoteLengthMessage], messages);
    }

    [Fact]
    public async Task Create_returns_400_when_the_product_does_not_exist()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var existingId = await CreateProductAsync(client);

        using var response = await PostInboundAsync(client, existingId + 1);
        var body = await ReadValidationProblemAsync(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var (key, messages) = Assert.Single(body.Errors);
        Assert.Equal("productId", key);
        Assert.Equal([ProductNotFoundMessage], messages);
    }

    [Fact]
    public async Task Create_returns_400_when_the_movement_date_is_after_today()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var productId = await CreateProductAsync(client);

        using var response = await PostInboundAsync(client, productId, movementDate: DaysFromToday(1));
        var body = await ReadValidationProblemAsync(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var (key, messages) = Assert.Single(body.Errors);
        Assert.Equal("movementDate", key);
        Assert.Equal([FutureDateMessage], messages);
    }

    // ---------- PUT /api/inventory-movements/{id}/inbound ----------

    [Fact]
    public async Task Update_returns_200_with_the_updated_inbound()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var productId = await CreateProductAsync(client);
        var created = await CreateInboundAsync(client, productId);
        var newDate = DateOnly.FromDateTime(DateTime.Now).AddDays(-2);

        using var response = await PutInboundAsync(client, created.Id, movementDate: newDate.ToString("yyyy-MM-dd"));
        var body = await response.Content.ReadFromJsonAsync<InboundBody>(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(created.Id, body.Id);
        Assert.Equal(productId, body.ProductId);
        Assert.Equal(UpdatedQuantity, body.Quantity);
        Assert.Equal(UpdatedTotalCost, body.TotalCost);
        Assert.Equal(newDate, body.MovementDate);
        Assert.Equal(UpdatedNote, body.Note);
        Assert.Equal(created.CreatedAt, body.CreatedAt);
    }

    [Fact]
    public async Task Update_sends_the_movement_date_as_year_month_day()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var productId = await CreateProductAsync(client);
        var created = await CreateInboundAsync(client, productId);
        var newDate = DaysFromToday(-2);

        using var response = await PutInboundAsync(client, created.Id, movementDate: newDate);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);

        Assert.Equal(newDate, body.GetProperty("movementDate").GetString());
    }

    [Fact]
    public async Task Update_sends_the_creation_date_in_utc()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var productId = await CreateProductAsync(client);
        var created = await CreateInboundAsync(client, productId);

        using var response = await PutInboundAsync(client, created.Id);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);

        var createdAt = body.GetProperty("createdAt").GetString();
        Assert.NotNull(createdAt);
        Assert.EndsWith("Z", createdAt);
    }

    [Theory]
    [InlineData("""{"quantity":30,"totalCost":22500,"movementDate":"{date}"}""")]
    [InlineData("""{"quantity":30,"totalCost":22500,"movementDate":"{date}","note":""}""")]
    public async Task Update_accepts_a_request_without_a_note_or_with_an_empty_note(string json)
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var productId = await CreateProductAsync(client);
        var created = await CreateInboundAsync(client, productId);

        using var response = await PutRawJsonAsync(client, created.Id, json.Replace("{date}", MovementDate));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(99999)]
    public async Task Update_accepts_a_quantity_at_the_limits_of_the_range(int quantity)
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var productId = await CreateProductAsync(client);
        var created = await CreateInboundAsync(client, productId);

        using var response = await PutInboundAsync(client, created.Id, quantity: quantity);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(9999999)]
    public async Task Update_accepts_a_total_cost_at_the_limits_of_the_range(int totalCost)
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var productId = await CreateProductAsync(client);
        var created = await CreateInboundAsync(client, productId);

        using var response = await PutInboundAsync(client, created.Id, totalCost: totalCost);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Update_accepts_a_note_of_500_characters()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var productId = await CreateProductAsync(client);
        var created = await CreateInboundAsync(client, productId);

        using var response = await PutInboundAsync(client, created.Id, note: new string('A', 500));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("""{"totalCost":22500,"movementDate":"{date}"}""", "quantity", QuantityRequiredMessage)]
    [InlineData("""{"quantity":null,"totalCost":22500,"movementDate":"{date}"}""", "quantity", QuantityRequiredMessage)]
    [InlineData("""{"quantity":30,"movementDate":"{date}"}""", "totalCost", TotalCostRequiredMessage)]
    [InlineData("""{"quantity":30,"totalCost":null,"movementDate":"{date}"}""", "totalCost", TotalCostRequiredMessage)]
    [InlineData("""{"quantity":30,"totalCost":22500}""", "movementDate", MovementDateRequiredMessage)]
    [InlineData("""{"quantity":30,"totalCost":22500,"movementDate":null}""", "movementDate", MovementDateRequiredMessage)]
    public async Task Update_returns_400_when_a_required_field_is_missing(string json, string field, string message)
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var productId = await CreateProductAsync(client);
        var inbound = await CreateInboundAsync(client, productId);

        using var response = await PutRawJsonAsync(client, inbound.Id, json.Replace("{date}", MovementDate));
        var body = await ReadValidationProblemAsync(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var (key, messages) = Assert.Single(body.Errors);
        Assert.Equal(field, key);
        Assert.Equal([message], messages);
    }

    [Fact]
    public async Task Update_returns_every_error_when_several_fields_fail()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var productId = await CreateProductAsync(client);
        var inbound = await CreateInboundAsync(client, productId);

        using var response = await PutRawJsonAsync(client, inbound.Id, "{}");
        var body = await ReadValidationProblemAsync(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(3, body.Errors.Count);
        Assert.Equal([QuantityRequiredMessage], body.Errors["quantity"]);
        Assert.Equal([TotalCostRequiredMessage], body.Errors["totalCost"]);
        Assert.Equal([MovementDateRequiredMessage], body.Errors["movementDate"]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(100000)]
    public async Task Update_returns_400_when_the_quantity_is_out_of_range(int quantity)
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var productId = await CreateProductAsync(client);
        var inbound = await CreateInboundAsync(client, productId);

        using var response = await PutInboundAsync(client, inbound.Id, quantity: quantity);
        var body = await ReadValidationProblemAsync(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var (key, messages) = Assert.Single(body.Errors);
        Assert.Equal("quantity", key);
        Assert.Equal([QuantityRangeMessage], messages);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(10000000)]
    public async Task Update_returns_400_when_the_total_cost_is_out_of_range(int totalCost)
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var productId = await CreateProductAsync(client);
        var inbound = await CreateInboundAsync(client, productId);

        using var response = await PutInboundAsync(client, inbound.Id, totalCost: totalCost);
        var body = await ReadValidationProblemAsync(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var (key, messages) = Assert.Single(body.Errors);
        Assert.Equal("totalCost", key);
        Assert.Equal([TotalCostRangeMessage], messages);
    }

    [Fact]
    public async Task Update_returns_400_when_the_note_is_longer_than_500_characters()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var productId = await CreateProductAsync(client);
        var inbound = await CreateInboundAsync(client, productId);

        using var response = await PutInboundAsync(client, inbound.Id, note: new string('A', 501));
        var body = await ReadValidationProblemAsync(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var (key, messages) = Assert.Single(body.Errors);
        Assert.Equal("note", key);
        Assert.Equal([NoteLengthMessage], messages);
    }

    [Fact]
    public async Task Update_returns_400_when_the_movement_date_is_after_today()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var productId = await CreateProductAsync(client);
        var inbound = await CreateInboundAsync(client, productId);

        using var response = await PutInboundAsync(client, inbound.Id, movementDate: DaysFromToday(1));
        var body = await ReadValidationProblemAsync(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var (key, messages) = Assert.Single(body.Errors);
        Assert.Equal("movementDate", key);
        Assert.Equal([FutureDateMessage], messages);
    }

    [Fact]
    public async Task Update_returns_400_when_the_new_movement_date_is_more_than_one_year_before_today()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var productId = await CreateProductAsync(client);
        var inbound = await CreateInboundAsync(client, productId);
        var oneYearAgo = DateOnly.FromDateTime(DateTime.Now).AddYears(-1);

        using var response = await PutInboundAsync(
            client, inbound.Id, movementDate: oneYearAgo.AddDays(-1).ToString("yyyy-MM-dd"));
        var body = await ReadValidationProblemAsync(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var (key, messages) = Assert.Single(body.Errors);
        Assert.Equal("movementDate", key);
        Assert.Equal([$"La fecha no puede ser anterior al {oneYearAgo:dd'/'MM'/'yyyy}."], messages);
    }

    [Fact]
    public async Task Update_does_not_change_the_inbound_when_the_request_is_rejected()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var productId = await CreateProductAsync(client);
        var inbound = await CreateInboundAsync(client, productId);

        using var response = await PutInboundAsync(client, inbound.Id, quantity: 0);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var listResponse = await GetAllAsync(client);
        var movement = Assert.Single(await ReadMovementsAsync(listResponse));
        Assert.Equal(Quantity, movement.Quantity);
        Assert.Equal(TotalCost, movement.Amount);
        Assert.Equal(Note, movement.Note);
    }

    [Fact]
    public async Task Update_returns_404_when_the_inbound_does_not_exist()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var productId = await CreateProductAsync(client);
        var inbound = await CreateInboundAsync(client, productId);

        using var response = await PutInboundAsync(client, inbound.Id + 1);
        var body = await response.Content.ReadFromJsonAsync<NotFoundBody>(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(InboundNotFoundMessage, body.Title);
        Assert.Equal(404, body.Status);
    }

    // There is no endpoint to register an outbound yet, so it is saved straight
    // into the API's database and its id is read from the movement list.
    [Fact]
    public async Task Update_returns_404_when_the_id_belongs_to_an_outbound()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var productId = await CreateProductAsync(client);
        await SeedMovementAsync(factory, productId, MovementType.Outbound);
        using var listResponse = await GetAllAsync(client);
        var outbound = Assert.Single(await ReadMovementsAsync(listResponse));

        using var response = await PutInboundAsync(client, outbound.Id);
        var body = await response.Content.ReadFromJsonAsync<NotFoundBody>(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(InboundNotFoundMessage, body.Title);
    }

    // ---------- GET /api/inventory-movements/stock-summary ----------

    [Fact]
    public async Task StockSummary_returns_200_with_the_stock_and_the_last_inbound()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var productId = await CreateProductAsync(client);
        using var first = await PostInboundAsync(client, productId, 24, 18000, DaysFromToday(-2));
        using var second = await PostInboundAsync(client, productId, 12, 9600, DaysFromToday(-1));

        using var response = await GetStockSummaryAsync(client, productId);
        var body = await response.Content.ReadFromJsonAsync<StockSummaryBody>(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(36, body.Stock);
        Assert.Equal(12, body.LastInboundQuantity);
        Assert.Equal(9600, body.LastInboundTotalCost);
    }

    [Fact]
    public async Task StockSummary_returns_404_when_the_product_does_not_exist()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var existingId = await CreateProductAsync(client);

        using var response = await GetStockSummaryAsync(client, existingId + 1);
        var body = await response.Content.ReadFromJsonAsync<NotFoundBody>(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(ProductNotFoundMessage, body.Title);
        Assert.Equal(404, body.Status);
    }

    [Fact]
    public async Task StockSummary_skips_the_excluded_movement_when_it_is_the_last_inbound()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var productId = await CreateProductAsync(client);
        using var first = await PostInboundAsync(client, productId, 24, 18000, DaysFromToday(-2));
        using var second = await PostInboundAsync(client, productId, 12, 9600, DaysFromToday(-1));
        var last = await second.Content.ReadFromJsonAsync<InboundBody>(TestContext.Current.CancellationToken);
        Assert.NotNull(last);

        using var response = await GetStockSummaryAsync(client, productId, last.Id);
        var body = await response.Content.ReadFromJsonAsync<StockSummaryBody>(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(24, body.LastInboundQuantity);
        Assert.Equal(18000, body.LastInboundTotalCost);
    }

    // ---------- GET /api/inventory-movements ----------

    [Fact]
    public async Task GetAll_returns_200_with_an_empty_list_when_there_are_no_movements()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        await CreateProductAsync(client);

        using var response = await GetAllAsync(client);
        var body = await ReadMovementsAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(body);
    }

    [Fact]
    public async Task GetAll_returns_200_with_the_movements_from_the_latest_to_the_earliest()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var productId = await CreateProductAsync(client);
        var first = await CreateInboundAsync(client, productId);
        var second = await CreateInboundAsync(client, productId);
        var third = await CreateInboundAsync(client, productId);

        using var response = await GetAllAsync(client);
        var body = await ReadMovementsAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new[] { third.Id, second.Id, first.Id }, body.Select(m => m.Id));
    }

    [Fact]
    public async Task GetAll_returns_the_fields_of_each_movement()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var productId = await CreateProductAsync(client);
        var created = await CreateInboundAsync(client, productId);

        using var response = await GetAllAsync(client);
        var movement = Assert.Single(await ReadMovementsAsync(response));

        Assert.Equal(created.Id, movement.Id);
        Assert.Equal(productId, movement.ProductId);
        Assert.Equal("TMV00001", movement.ProductCode);
        Assert.Equal("Arroz 1 kg", movement.ProductName);
        Assert.Equal(Quantity, movement.Quantity);
        Assert.Equal(TotalCost, movement.Amount);
        Assert.Equal(Yesterday, movement.MovementDate);
        Assert.Equal(Note, movement.Note);
        Assert.Equal(created.CreatedAt, movement.CreatedAt);
    }

    // There is no endpoint to register an outbound yet, so the movement is saved
    // straight into the API's database.
    [Theory]
    [InlineData(MovementType.Inbound, "Inbound")]
    [InlineData(MovementType.Outbound, "Outbound")]
    public async Task GetAll_sends_the_type_as_its_name(MovementType type, string expected)
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var productId = await CreateProductAsync(client);
        await SeedMovementAsync(factory, productId, type);

        using var response = await GetAllAsync(client);
        var movement = Assert.Single(await ReadMovementsAsync(response));

        Assert.Equal(expected, movement.Type);
    }

    [Fact]
    public async Task GetAll_sends_the_movement_date_as_year_month_day()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var productId = await CreateProductAsync(client);
        await CreateInboundAsync(client, productId);

        using var response = await GetAllAsync(client);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);

        Assert.Equal(MovementDate, body[0].GetProperty("movementDate").GetString());
    }

    [Fact]
    public async Task GetAll_sends_the_creation_date_in_utc()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var productId = await CreateProductAsync(client);
        await CreateInboundAsync(client, productId);

        using var response = await GetAllAsync(client);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);

        var createdAt = body[0].GetProperty("createdAt").GetString();
        Assert.NotNull(createdAt);
        Assert.EndsWith("Z", createdAt);
    }

    // ---------- GET /api/inventory-movements/{id} ----------

    [Fact]
    public async Task GetById_returns_200_with_the_fields_of_the_movement()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var productId = await CreateProductAsync(client);
        var created = await CreateInboundAsync(client, productId);

        using var response = await GetByIdAsync(client, created.Id);
        var movement = await response.Content.ReadFromJsonAsync<MovementBody>(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(movement);
        Assert.Equal(created.Id, movement.Id);
        Assert.Equal(productId, movement.ProductId);
        Assert.Equal("TMV00001", movement.ProductCode);
        Assert.Equal("Arroz 1 kg", movement.ProductName);
        Assert.Equal("Inbound", movement.Type);
        Assert.Equal(Quantity, movement.Quantity);
        Assert.Equal(TotalCost, movement.Amount);
        Assert.Equal(Yesterday, movement.MovementDate);
        Assert.Equal(Note, movement.Note);
        Assert.Equal(created.CreatedAt, movement.CreatedAt);
    }

    // There is no endpoint to register an outbound yet, so the movement is saved
    // straight into the API's database and its id is read from the movement list.
    [Theory]
    [InlineData(MovementType.Inbound, "Inbound")]
    [InlineData(MovementType.Outbound, "Outbound")]
    public async Task GetById_sends_the_type_as_its_name(MovementType type, string expected)
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var productId = await CreateProductAsync(client);
        await SeedMovementAsync(factory, productId, type);
        using var listResponse = await GetAllAsync(client);
        var seeded = Assert.Single(await ReadMovementsAsync(listResponse));

        using var response = await GetByIdAsync(client, seeded.Id);
        var movement = await response.Content.ReadFromJsonAsync<MovementBody>(TestContext.Current.CancellationToken);

        Assert.NotNull(movement);
        Assert.Equal(expected, movement.Type);
    }

    [Fact]
    public async Task GetById_sends_the_movement_date_as_year_month_day()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var productId = await CreateProductAsync(client);
        var created = await CreateInboundAsync(client, productId);

        using var response = await GetByIdAsync(client, created.Id);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);

        Assert.Equal(MovementDate, body.GetProperty("movementDate").GetString());
    }

    [Fact]
    public async Task GetById_sends_the_creation_date_in_utc()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var productId = await CreateProductAsync(client);
        var created = await CreateInboundAsync(client, productId);

        using var response = await GetByIdAsync(client, created.Id);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);

        var createdAt = body.GetProperty("createdAt").GetString();
        Assert.NotNull(createdAt);
        Assert.EndsWith("Z", createdAt);
    }

    [Fact]
    public async Task GetById_returns_404_when_the_movement_does_not_exist()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var productId = await CreateProductAsync(client);
        var created = await CreateInboundAsync(client, productId);

        using var response = await GetByIdAsync(client, created.Id + 1);
        var body = await response.Content.ReadFromJsonAsync<NotFoundBody>(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(MovementNotFoundMessage, body.Title);
        Assert.Equal(404, body.Status);
    }

    private static Task<HttpResponseMessage> GetByIdAsync(HttpClient client, int id) =>
        client.GetAsync($"{MovementsUrl}/{id}", TestContext.Current.CancellationToken);

    private static Task<HttpResponseMessage> GetAllAsync(HttpClient client) =>
        client.GetAsync(MovementsUrl, TestContext.Current.CancellationToken);

    private static async Task<MovementBody[]> ReadMovementsAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<MovementBody[]>(TestContext.Current.CancellationToken);
        Assert.NotNull(body);
        return body;
    }

    private static async Task<InboundBody> CreateInboundAsync(HttpClient client, int productId)
    {
        using var response = await PostInboundAsync(client, productId);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<InboundBody>(TestContext.Current.CancellationToken);
        Assert.NotNull(body);
        return body;
    }

    private static async Task SeedMovementAsync(ApiFactory factory, int productId, MovementType type)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MarketAppDbContext>();
        context.Set<InventoryMovement>().Add(new InventoryMovement
        {
            ProductId = productId,
            Type = type,
            Quantity = 5,
            Amount = 6250,
            MovementDate = Yesterday,
            CreatedAt = DateTime.UtcNow,
            IsVoided = false,
        });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static string DaysFromToday(int days) =>
        DateOnly.FromDateTime(DateTime.Now).AddDays(days).ToString("yyyy-MM-dd");

    private static async Task<int> CreateProductAsync(HttpClient client)
    {
        using var response = await client.PostAsJsonAsync(
            ProductsUrl,
            new { code = "TMV00001", name = "Arroz 1 kg", salePrice = 1250 },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ProductBody>(TestContext.Current.CancellationToken);
        Assert.NotNull(body);
        return body.Id;
    }

    private static Task<HttpResponseMessage> PostInboundAsync(
        HttpClient client,
        int productId,
        int quantity = Quantity,
        int totalCost = TotalCost,
        string? movementDate = null,
        string note = Note) =>
        client.PostAsJsonAsync(
            InboundUrl,
            new { productId, quantity, totalCost, movementDate = movementDate ?? MovementDate, note },
            TestContext.Current.CancellationToken);

    private static string UpdateInboundUrl(int id) => $"{MovementsUrl}/{id}/inbound";

    private static Task<HttpResponseMessage> PutInboundAsync(
        HttpClient client,
        int id,
        int quantity = UpdatedQuantity,
        int totalCost = UpdatedTotalCost,
        string? movementDate = null,
        string note = UpdatedNote) =>
        client.PutAsJsonAsync(
            UpdateInboundUrl(id),
            new { quantity, totalCost, movementDate = movementDate ?? MovementDate, note },
            TestContext.Current.CancellationToken);

    private static async Task<HttpResponseMessage> PutRawJsonAsync(HttpClient client, int id, string json)
    {
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await client.PutAsync(UpdateInboundUrl(id), content, TestContext.Current.CancellationToken);
    }

    private static Task<HttpResponseMessage> GetStockSummaryAsync(
        HttpClient client, int productId, int? excludedMovementId = null) =>
        client.GetAsync(
            excludedMovementId is null
                ? $"{StockSummaryUrl}?productId={productId}"
                : $"{StockSummaryUrl}?productId={productId}&excludedMovementId={excludedMovementId}",
            TestContext.Current.CancellationToken);

    private static async Task<HttpResponseMessage> PostRawJsonAsync(HttpClient client, string json)
    {
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await client.PostAsync(InboundUrl, content, TestContext.Current.CancellationToken);
    }

    private static async Task<ValidationProblemBody> ReadValidationProblemAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<ValidationProblemBody>(
            TestContext.Current.CancellationToken);
        Assert.NotNull(body);
        return body;
    }

    private sealed record ProductBody(int Id);

    private sealed record InboundBody(
        int Id,
        int ProductId,
        int Quantity,
        int TotalCost,
        DateOnly MovementDate,
        string? Note,
        DateTime CreatedAt);

    private sealed record ValidationProblemBody(Dictionary<string, string[]> Errors);

    private sealed record StockSummaryBody(int Stock, int? LastInboundQuantity, int? LastInboundTotalCost);

    private sealed record NotFoundBody(string Title, int Status);

    private sealed record MovementBody(
        int Id,
        int ProductId,
        string ProductCode,
        string ProductName,
        string Type,
        int Quantity,
        int Amount,
        DateOnly MovementDate,
        string? Note,
        DateTime CreatedAt);
}
