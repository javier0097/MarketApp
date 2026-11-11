using MarketApp.Api.Tests.Support;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace MarketApp.Api.Tests.ApiTests;

public class InventoryMovementsApiTests
{
    private const string InboundUrl = "/api/inventory-movements/inbound";
    private const string StockSummaryUrl = "/api/inventory-movements/stock-summary";
    private const string ProductsUrl = "/api/products";

    private const string ProductRequiredMessage =
        "Falta el producto. Búsquelo por nombre o código, o escanee su código de barras.";
    private const string QuantityRequiredMessage = "Ingrese la cantidad de unidades del lote.";
    private const string QuantityRangeMessage = "La cantidad debe estar entre 1 y 9.999.";
    private const string TotalCostRequiredMessage = "Ingrese el costo total del lote.";
    private const string TotalCostRangeMessage = "El costo total debe estar entre Bs 0,00 y Bs 99.999,99.";
    private const string MovementDateRequiredMessage = "Ingrese la fecha de ingreso.";
    private const string NoteLengthMessage = "La nota no puede tener más de 500 caracteres.";
    private const string ProductNotFoundMessage = "El producto no existe.";
    private const string FutureDateMessage = "La fecha no puede ser posterior a hoy.";

    private const int Quantity = 24;
    private const int TotalCost = 18000;
    private const string MovementDate = "2026-11-01";
    private const string Note = "Compra al proveedor";

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
        Assert.Equal(new DateOnly(2026, 11, 1), body.MovementDate);
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
    [InlineData("""{"productId":1,"quantity":24,"totalCost":18000,"movementDate":"2026-11-01"}""")]
    [InlineData("""{"productId":1,"quantity":24,"totalCost":18000,"movementDate":"2026-11-01","note":""}""")]
    public async Task Create_accepts_a_request_without_a_note_or_with_an_empty_note(string json)
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        await CreateProductAsync(client);

        using var response = await PostRawJsonAsync(client, json);

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
    [InlineData(9999)]
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
    [InlineData(10000)]
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

        using var response = await PostInboundAsync(client, productId, movementDate: Tomorrow());
        var body = await ReadValidationProblemAsync(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var (key, messages) = Assert.Single(body.Errors);
        Assert.Equal("movementDate", key);
        Assert.Equal([FutureDateMessage], messages);
    }

    // ---------- GET /api/inventory-movements/stock-summary ----------

    [Fact]
    public async Task StockSummary_returns_200_with_the_stock_and_the_last_inbound()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var productId = await CreateProductAsync(client);
        using var first = await PostInboundAsync(client, productId, 24, 18000, "2026-11-01");
        using var second = await PostInboundAsync(client, productId, 12, 9600, "2026-11-02");

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

    private static string Tomorrow() =>
        DateOnly.FromDateTime(DateTime.Now).AddDays(1).ToString("yyyy-MM-dd");

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
        string movementDate = MovementDate,
        string note = Note) =>
        client.PostAsJsonAsync(
            InboundUrl,
            new { productId, quantity, totalCost, movementDate, note },
            TestContext.Current.CancellationToken);

    private static Task<HttpResponseMessage> GetStockSummaryAsync(HttpClient client, int productId) =>
        client.GetAsync($"{StockSummaryUrl}?productId={productId}", TestContext.Current.CancellationToken);

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
}
