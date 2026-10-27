using MarketApp.Api.Tests.Support;
using System.Net;
using System.Net.Http.Json;
using System.Text;

namespace MarketApp.Api.Tests.ApiTests;

public class ProductsApiTests
{
    private const string ProductsUrl = "/api/products";
    private const string NextInternalCodeUrl = "/api/products/next-internal-code";
    private const string PriceRangeMessage = "El precio de venta debe estar entre Bs 0,01 y Bs 9.999,99.";
    private const string InvalidBarcodeMessage =
        "El código de barras no es válido. Revise que esté bien escrito o marque \"Sin código de barras\".";

    // EAN-13 4006381333931: 4+0+0+18+3+24+1+9+3+9+9+9 = 89 -> check 1 (valid).
    private const string ValidBarcode = "4006381333931";

    private const string BarcodeWithWrongCheckDigit = "4006381333932";

    // ---------- POST /api/products ----------

    [Fact]
    public async Task Create_returns_201_with_the_created_product()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        using var response = await PostProductAsync(client, ValidBarcode, "Leche entera 1 L", 1250);
        var body = await response.Content.ReadFromJsonAsync<ProductBody>(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(body);
        Assert.True(body.Id > 0);
        Assert.Equal(ValidBarcode, body.Code);
        Assert.Equal("Leche entera 1 L", body.Name);
        Assert.Equal(1250, body.SalePrice);
    }

    [Fact]
    public async Task Create_accepts_a_sale_price_of_one_cent()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        using var response = await PostProductAsync(client, ValidBarcode, "Leche entera 1 L", 1);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Create_accepts_the_highest_sale_price()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        using var response = await PostProductAsync(client, ValidBarcode, "Leche entera 1 L", 999999);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Theory]
    [InlineData("""{"name":"Leche entera 1 L","salePrice":1250}""", "code", "Ingrese el código del producto.")]
    [InlineData("""{"code":null,"name":"Leche entera 1 L","salePrice":1250}""", "code", "Ingrese el código del producto.")]
    [InlineData("""{"code":"","name":"Leche entera 1 L","salePrice":1250}""", "code", "Ingrese el código del producto.")]
    [InlineData("""{"code":"   ","name":"Leche entera 1 L","salePrice":1250}""", "code", "Ingrese el código del producto.")]
    [InlineData("""{"code":"4006381333931","salePrice":1250}""", "name", "Ingrese el nombre del producto.")]
    [InlineData("""{"code":"4006381333931","name":null,"salePrice":1250}""", "name", "Ingrese el nombre del producto.")]
    [InlineData("""{"code":"4006381333931","name":"","salePrice":1250}""", "name", "Ingrese el nombre del producto.")]
    [InlineData("""{"code":"4006381333931","name":"   ","salePrice":1250}""", "name", "Ingrese el nombre del producto.")]
    [InlineData("""{"code":"4006381333931","name":"Leche entera 1 L"}""", "salePrice", "Ingrese el precio de venta.")]
    [InlineData("""{"code":"4006381333931","name":"Leche entera 1 L","salePrice":null}""", "salePrice", "Ingrese el precio de venta.")]
    public async Task Create_returns_400_when_a_required_field_is_missing(string json, string field, string message)
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        using var response = await PostRawJsonAsync(client, json);
        var body = await ReadValidationProblemAsync(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var (key, messages) = Assert.Single(body.Errors);
        Assert.Equal(field, key);
        Assert.Equal([message], messages);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Create_returns_400_when_the_sale_price_is_not_greater_than_zero(int salePrice)
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        using var response = await PostProductAsync(client, ValidBarcode, "Leche entera 1 L", salePrice);
        var body = await ReadValidationProblemAsync(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var (key, messages) = Assert.Single(body.Errors);
        Assert.Equal("salePrice", key);
        Assert.Equal([PriceRangeMessage], messages);
    }

    [Fact]
    public async Task Create_returns_400_when_the_sale_price_is_above_the_highest()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        using var response = await PostProductAsync(client, ValidBarcode, "Leche entera 1 L", 1000000);
        var body = await ReadValidationProblemAsync(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var (key, messages) = Assert.Single(body.Errors);
        Assert.Equal("salePrice", key);
        Assert.Equal([PriceRangeMessage], messages);
    }

    [Fact]
    public async Task Create_accepts_a_name_of_200_characters()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        using var response = await PostProductAsync(client, ValidBarcode, new string('A', 200), 1250);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Create_returns_400_when_the_name_is_longer_than_200_characters()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        using var response = await PostProductAsync(client, ValidBarcode, new string('A', 201), 1250);
        var body = await ReadValidationProblemAsync(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var (key, messages) = Assert.Single(body.Errors);
        Assert.Equal("name", key);
        Assert.Equal(["El nombre no puede tener más de 200 caracteres."], messages);
    }

    [Fact]
    public async Task Create_returns_every_error_when_several_fields_fail()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        using var response = await PostRawJsonAsync(client, "{}");
        var body = await ReadValidationProblemAsync(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(3, body.Errors.Count);
        Assert.Equal(["Ingrese el código del producto."], body.Errors["code"]);
        Assert.Equal(["Ingrese el nombre del producto."], body.Errors["name"]);
        Assert.Equal(["Ingrese el precio de venta."], body.Errors["salePrice"]);
    }

    // The barcode is invalid and the price is 0. Request validation fails first, so the service
    // validations do not run: only the price error comes back, not the barcode one.
    [Fact]
    public async Task Create_returns_only_request_errors_when_request_validation_fails()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        using var response = await PostProductAsync(client, BarcodeWithWrongCheckDigit, "Leche entera 1 L", 0);
        var body = await ReadValidationProblemAsync(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var (key, messages) = Assert.Single(body.Errors);
        Assert.Equal("salePrice", key);
        Assert.Equal([PriceRangeMessage], messages);
    }

    [Fact]
    public async Task Create_returns_400_when_the_code_already_exists()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        using var first = await PostProductAsync(client, ValidBarcode, "Leche entera 1 L", 1250);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        using var response = await PostProductAsync(client, ValidBarcode, "Leche deslactosada 1 L", 1400);
        var body = await ReadValidationProblemAsync(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var (key, messages) = Assert.Single(body.Errors);
        Assert.Equal("code", key);
        Assert.Equal(["Ya existe un producto con este código."], messages);
    }

    [Fact]
    public async Task Create_returns_400_when_the_barcode_is_invalid()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        using var response = await PostProductAsync(client, BarcodeWithWrongCheckDigit, "Leche entera 1 L", 1250);
        var body = await ReadValidationProblemAsync(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var (key, messages) = Assert.Single(body.Errors);
        Assert.Equal("code", key);
        Assert.Equal([InvalidBarcodeMessage], messages);
    }

    // ---------- GET /api/products/next-internal-code ----------

    [Fact]
    public async Task Returns_200_with_TMV00001_when_there_are_no_products()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(NextInternalCodeUrl, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<CodeBody>(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal("TMV00001", body.Code);
    }

    [Fact]
    public async Task Returns_the_code_after_the_one_used_by_a_created_product()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        using var created = await PostProductAsync(client, "TMV00001", "Arroz 1 kg", 1250);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var code = await GetNextInternalCodeAsync(client);

        Assert.Equal("TMV00002", code);
    }

    [Fact]
    public async Task Returns_a_code_that_can_be_used_to_create_a_product()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        using var first = await PostProductAsync(client, "TMV00001", "Arroz 1 kg", 1250);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        var code = await GetNextInternalCodeAsync(client);

        using var response = await PostProductAsync(client, code, "Arroz 1 kg", 1250);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static Task<HttpResponseMessage> PostProductAsync(HttpClient client, string code, string name, int salePrice) =>
        client.PostAsJsonAsync(
            ProductsUrl,
            new { code, name, salePrice },
            TestContext.Current.CancellationToken);

    private static async Task<HttpResponseMessage> PostRawJsonAsync(HttpClient client, string json)
    {
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await client.PostAsync(ProductsUrl, content, TestContext.Current.CancellationToken);
    }

    private static async Task<string> GetNextInternalCodeAsync(HttpClient client)
    {
        using var response = await client.GetAsync(NextInternalCodeUrl, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CodeBody>(TestContext.Current.CancellationToken);
        Assert.NotNull(body);
        return body.Code;
    }

    private static async Task<ValidationProblemBody> ReadValidationProblemAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<ValidationProblemBody>(
            TestContext.Current.CancellationToken);
        Assert.NotNull(body);
        return body;
    }

    private sealed record ProductBody(int Id, string Code, string Name, int SalePrice);

    private sealed record ValidationProblemBody(Dictionary<string, string[]> Errors);

    private sealed record CodeBody(string Code);
}
