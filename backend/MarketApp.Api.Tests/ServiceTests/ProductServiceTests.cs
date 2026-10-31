using MarketApp.Api.Dtos.Requests;
using MarketApp.Api.Dtos.Responses;
using MarketApp.Api.Entities;
using MarketApp.Api.Exceptions;
using MarketApp.Api.Services;
using MarketApp.Api.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace MarketApp.Api.Tests.ServiceTests;

public class ProductServiceTests
{
    private const string DuplicateCodeMessage = "Ya existe un producto con este código.";
    private const string InternalCodeMessage = "Los códigos internos los asigna el sistema.";
    private const string InvalidBarcodeMessage =
        "El código de barras no es válido. Revise que esté bien escrito o marque \"Sin código de barras\".";

    // EAN-13 4006381333931: 4+0+0+18+3+24+1+9+3+9+9+9 = 89 -> check 1 (valid).
    private const string ValidBarcode = "4006381333931";

    // Same code with the last digit changed: expected check 1, not 2.
    private const string BarcodeWithWrongCheckDigit = "4006381333932";

    // ---------- GetNextInternalCodeAsync ----------

    [Fact]
    public async Task Returns_TMV00001_when_there_are_no_products()
    {
        using var database = new TestDatabase();
        using var context = database.CreateContext();
        var service = new ProductService(context, NullLogger<ProductService>.Instance);

        var code = await service.GetNextInternalCodeAsync();

        Assert.Equal("TMV00001", code);
    }

    [Fact]
    public async Task Returns_TMV00002_when_the_only_product_has_code_TMV00001()
    {
        using var database = new TestDatabase();
        await SeedProductsAsync(database, NewProduct("TMV00001"));
        using var context = database.CreateContext();
        var service = new ProductService(context, NullLogger<ProductService>.Instance);

        var code = await service.GetNextInternalCodeAsync();

        Assert.Equal("TMV00002", code);
    }

    [Fact]
    public async Task Returns_the_code_after_the_last_one_when_there_are_several_products()
    {
        using var database = new TestDatabase();
        await SeedProductsAsync(database, NewProduct("TMV00001"), NewProduct("TMV00002"), NewProduct("TMV00003"));
        using var context = database.CreateContext();
        var service = new ProductService(context, NullLogger<ProductService>.Instance);

        var code = await service.GetNextInternalCodeAsync();

        Assert.Equal("TMV00004", code);
    }

    [Fact]
    public async Task Returns_the_code_after_the_highest_one_regardless_of_insertion_order()
    {
        using var database = new TestDatabase();
        await SeedProductsAsync(database, NewProduct("TMV00003"), NewProduct("TMV00001"), NewProduct("TMV00002"));
        using var context = database.CreateContext();
        var service = new ProductService(context, NullLogger<ProductService>.Instance);

        var code = await service.GetNextInternalCodeAsync();

        Assert.Equal("TMV00004", code);
    }

    [Fact]
    public async Task Does_not_fill_a_gap_in_the_numbering()
    {
        using var database = new TestDatabase();
        await SeedProductsAsync(database, NewProduct("TMV00001"), NewProduct("TMV00003"));
        using var context = database.CreateContext();
        var service = new ProductService(context, NullLogger<ProductService>.Instance);

        var code = await service.GetNextInternalCodeAsync();

        Assert.Equal("TMV00004", code);
    }

    [Fact]
    public async Task Continues_from_the_highest_code_when_numbering_does_not_start_at_one()
    {
        using var database = new TestDatabase();
        await SeedProductsAsync(database, NewProduct("TMV00005"));
        using var context = database.CreateContext();
        var service = new ProductService(context, NullLogger<ProductService>.Instance);

        var code = await service.GetNextInternalCodeAsync();

        Assert.Equal("TMV00006", code);
    }

    [Theory]
    [InlineData(9, "TMV00010")]
    [InlineData(99, "TMV00100")]
    [InlineData(999, "TMV01000")]
    public async Task Keeps_five_digits_padded_with_zeros_when_the_number_gains_a_digit(
        int existingProducts, string expected)
    {
        using var database = new TestDatabase();
        var products = Enumerable.Range(1, existingProducts)
            .Select(number => NewProduct($"TMV{number:D5}"))
            .ToArray();
        await SeedProductsAsync(database, products);
        using var context = database.CreateContext();
        var service = new ProductService(context, NullLogger<ProductService>.Instance);

        var code = await service.GetNextInternalCodeAsync();

        Assert.Equal(expected, code);
    }

    [Fact]
    public async Task Ignores_products_whose_code_is_not_an_internal_code()
    {
        using var database = new TestDatabase();
        await SeedProductsAsync(
            database,
            NewProduct("TMV00002"),
            NewProduct("4006381333931"),
            NewProduct("96385074"));
        using var context = database.CreateContext();
        var service = new ProductService(context, NullLogger<ProductService>.Instance);

        var code = await service.GetNextInternalCodeAsync();

        Assert.Equal("TMV00003", code);
    }

    [Fact]
    public async Task Returns_TMV00001_when_no_product_has_an_internal_code()
    {
        using var database = new TestDatabase();
        await SeedProductsAsync(database, NewProduct("4006381333931"), NewProduct("96385074"));
        using var context = database.CreateContext();
        var service = new ProductService(context, NullLogger<ProductService>.Instance);

        var code = await service.GetNextInternalCodeAsync();

        Assert.Equal("TMV00001", code);
    }

    [Fact]
    public async Task Takes_inactive_products_into_account()
    {
        using var database = new TestDatabase();
        await SeedProductsAsync(database, NewProduct("TMV00001"), NewProduct("TMV00005", isActive: false));
        using var context = database.CreateContext();
        var service = new ProductService(context, NullLogger<ProductService>.Instance);

        var code = await service.GetNextInternalCodeAsync();

        Assert.Equal("TMV00006", code);
    }

    [Fact]
    public async Task Returns_the_same_code_when_called_twice_without_creating_a_product()
    {
        using var database = new TestDatabase();
        await SeedProductsAsync(database, NewProduct("TMV00001"));
        using var context = database.CreateContext();
        var service = new ProductService(context, NullLogger<ProductService>.Instance);

        var first = await service.GetNextInternalCodeAsync();
        var second = await service.GetNextInternalCodeAsync();

        Assert.Equal("TMV00002", first);
        Assert.Equal("TMV00002", second);
    }

    [Fact]
    public async Task Does_not_change_the_products_table()
    {
        using var database = new TestDatabase();
        await SeedProductsAsync(database, NewProduct("TMV00001"), NewProduct("TMV00002"));
        using (var context = database.CreateContext())
        {
            var service = new ProductService(context, NullLogger<ProductService>.Instance);

            await service.GetNextInternalCodeAsync();
        }

        using var verification = database.CreateContext();
        var codes = await verification.Products
            .Select(product => product.Code)
            .OrderBy(code => code)
            .ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(["TMV00001", "TMV00002"], codes);
    }

    // ---------- CreateAsync ----------

    [Fact]
    public async Task Returns_the_created_product()
    {
        using var database = new TestDatabase();

        var response = await CreateProductAsync(database, NewRequest(ValidBarcode, "Leche entera 1 L", 1250));

        Assert.True(response.Id > 0);
        Assert.Equal(ValidBarcode, response.Code);
        Assert.Equal("Leche entera 1 L", response.Name);
        Assert.Equal(1250, response.SalePrice);
    }

    [Fact]
    public async Task Saves_the_code_name_and_sale_price()
    {
        using var database = new TestDatabase();

        var response = await CreateProductAsync(database, NewRequest(ValidBarcode, "Leche entera 1 L", 1250));

        var product = await FindProductAsync(database, response.Id);
        Assert.Equal(ValidBarcode, product.Code);
        Assert.Equal("Leche entera 1 L", product.Name);
        Assert.Equal(1250, product.SalePrice);
    }

    [Fact]
    public async Task Saves_the_minimum_stock_as_zero()
    {
        using var database = new TestDatabase();

        var response = await CreateProductAsync(database, NewRequest(ValidBarcode));

        var product = await FindProductAsync(database, response.Id);
        Assert.Equal(0, product.MinStock);
    }

    [Fact]
    public async Task Saves_the_product_as_active()
    {
        using var database = new TestDatabase();

        var response = await CreateProductAsync(database, NewRequest(ValidBarcode));

        var product = await FindProductAsync(database, response.Id);
        Assert.True(product.IsActive);
    }

    [Fact]
    public async Task Leaves_created_by_and_updated_by_empty()
    {
        using var database = new TestDatabase();

        var response = await CreateProductAsync(database, NewRequest(ValidBarcode));

        var product = await FindProductAsync(database, response.Id);
        Assert.Null(product.CreatedById);
        Assert.Null(product.UpdatedById);
    }

    [Fact]
    public async Task Sets_the_creation_date_to_the_current_utc_time()
    {
        using var database = new TestDatabase();

        var before = DateTime.UtcNow;
        var response = await CreateProductAsync(database, NewRequest(ValidBarcode));
        var after = DateTime.UtcNow;

        var product = await FindProductAsync(database, response.Id);
        Assert.InRange(product.CreatedAt, before, after);
    }

    [Fact]
    public async Task Sets_the_update_date_equal_to_the_creation_date()
    {
        using var database = new TestDatabase();

        var response = await CreateProductAsync(database, NewRequest(ValidBarcode));

        var product = await FindProductAsync(database, response.Id);
        Assert.Equal(product.CreatedAt, product.UpdatedAt);
    }

    [Fact]
    public async Task Accepts_the_next_internal_code()
    {
        using var database = new TestDatabase();
        await SeedProductsAsync(database, NewProduct("TMV00001"));

        var response = await CreateProductAsync(database, NewRequest("TMV00002"));

        var product = await FindProductAsync(database, response.Id);
        Assert.Equal("TMV00002", product.Code);
    }

    // One case only, to check that the barcode validation is reached (BarcodeHelperTests covers the rest).
    [Fact]
    public async Task Rejects_an_invalid_barcode()
    {
        using var database = new TestDatabase();
        using var context = database.CreateContext();
        var service = new ProductService(context, NullLogger<ProductService>.Instance);

        var exception = await Assert.ThrowsAsync<FieldValidationException>(
            () => service.CreateAsync(NewRequest(BarcodeWithWrongCheckDigit)));

        Assert.Equal("Code", exception.Field);
        Assert.Equal(InvalidBarcodeMessage, exception.Message);
    }

    //One case only, to check that the internal code validation is reached (GetNextInternalCodeAsyncTests covers the rest).
    [Fact]
    public async Task Rejects_an_internal_code_that_is_not_the_next_one()
    {
        using var database = new TestDatabase();
        await SeedProductsAsync(database, NewProduct("TMV00001"));
        using var context = database.CreateContext();
        var service = new ProductService(context, NullLogger<ProductService>.Instance);

        var exception = await Assert.ThrowsAsync<FieldValidationException>(
            () => service.CreateAsync(NewRequest("TMV00007")));

        Assert.Equal("Code", exception.Field);
        Assert.Equal(InternalCodeMessage, exception.Message);
    }

    [Fact]
    public async Task Rejects_a_code_that_another_product_already_has()
    {
        using var database = new TestDatabase();
        await SeedProductsAsync(database, NewProduct(ValidBarcode));
        using var context = database.CreateContext();
        var service = new ProductService(context, NullLogger<ProductService>.Instance);

        var exception = await Assert.ThrowsAsync<FieldValidationException>(
            () => service.CreateAsync(NewRequest(ValidBarcode)));

        Assert.Equal("Code", exception.Field);
        Assert.Equal(DuplicateCodeMessage, exception.Message);
    }

    [Fact]
    public async Task Rejects_a_code_that_an_inactive_product_already_has()
    {
        using var database = new TestDatabase();
        await SeedProductsAsync(database, NewProduct(ValidBarcode, isActive: false));
        using var context = database.CreateContext();
        var service = new ProductService(context, NullLogger<ProductService>.Instance);

        var exception = await Assert.ThrowsAsync<FieldValidationException>(
            () => service.CreateAsync(NewRequest(ValidBarcode)));

        Assert.Equal("Code", exception.Field);
        Assert.Equal(DuplicateCodeMessage, exception.Message);
    }

    // TMV00001 already exists: it is both a duplicate and not the next internal code (TMV00002).
    // The duplicate error wins.
    [Fact]
    public async Task Reports_an_existing_internal_code_as_a_duplicate()
    {
        using var database = new TestDatabase();
        await SeedProductsAsync(database, NewProduct("TMV00001"));
        using var context = database.CreateContext();
        var service = new ProductService(context, NullLogger<ProductService>.Instance);

        var exception = await Assert.ThrowsAsync<FieldValidationException>(
            () => service.CreateAsync(NewRequest("TMV00001")));

        Assert.Equal("Code", exception.Field);
        Assert.Equal(DuplicateCodeMessage, exception.Message);
    }

    // The table starts with two products (TMV00001 and the valid barcode); after a rejected creation
    // (bad check digit, wrong internal code, duplicate barcode, duplicate internal code) it still has two.
    [Theory]
    [InlineData(BarcodeWithWrongCheckDigit)]
    [InlineData("TMV00007")]
    [InlineData(ValidBarcode)]
    [InlineData("TMV00001")]
    public async Task Does_not_save_a_product_that_fails_validation(string code)
    {
        using var database = new TestDatabase();
        await SeedProductsAsync(database, NewProduct("TMV00001"), NewProduct(ValidBarcode));
        using (var context = database.CreateContext())
        {
            var service = new ProductService(context, NullLogger<ProductService>.Instance);

            await Assert.ThrowsAsync<FieldValidationException>(() => service.CreateAsync(NewRequest(code)));
        }

        using var verification = database.CreateContext();
        var count = await verification.Products.CountAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, count);
    }

    // ---------- GetActiveAsync ----------

    [Fact]
    public async Task Returns_an_empty_list_when_there_are_no_products()
    {
        using var database = new TestDatabase();
        using var context = database.CreateContext();
        var service = new ProductService(context, NullLogger<ProductService>.Instance);

        var products = await service.GetActiveAsync();

        Assert.Empty(products);
    }

    [Fact]
    public async Task Returns_an_empty_list_when_all_products_are_inactive()
    {
        using var database = new TestDatabase();
        await SeedProductsAsync(
            database,
            NewProduct("TMV00001", isActive: false),
            NewProduct("TMV00002", isActive: false));
        using var context = database.CreateContext();
        var service = new ProductService(context, NullLogger<ProductService>.Instance);

        var products = await service.GetActiveAsync();

        Assert.Empty(products);
    }

    [Fact]
    public async Task Returns_only_the_active_products()
    {
        using var database = new TestDatabase();
        await SeedProductsAsync(
            database,
            NewProduct("TMV00001"),
            NewProduct("TMV00002", isActive: false),
            NewProduct("TMV00003"));
        using var context = database.CreateContext();
        var service = new ProductService(context, NullLogger<ProductService>.Instance);

        var products = await service.GetActiveAsync();

        var codes = products.Select(product => product.Code).Order().ToList();
        Assert.Equal(["TMV00001", "TMV00003"], codes);
    }

    [Fact]
    public async Task Orders_the_products_from_the_highest_to_the_lowest_id()
    {
        using var database = new TestDatabase();
        var seeded = new[] { NewProduct("TMV00001"), NewProduct("TMV00002"), NewProduct("TMV00003") };
        await SeedProductsAsync(database, seeded);
        using var context = database.CreateContext();
        var service = new ProductService(context, NullLogger<ProductService>.Instance);

        var products = await service.GetActiveAsync();

        var expectedIds = seeded.Select(product => product.Id).OrderDescending().ToList();
        var ids = products.Select(product => product.Id).ToList();
        Assert.Equal(expectedIds, ids);
    }

    [Fact]
    public async Task Returns_the_id_code_name_sale_price_and_update_date_of_the_product()
    {
        using var database = new TestDatabase();
        var updatedAt = new DateTime(2026, 5, 15, 9, 30, 0, DateTimeKind.Utc);
        var seeded = new Product
        {
            Code = ValidBarcode,
            Name = "Leche entera 1 L",
            SalePrice = 1250,
            CreatedAt = new DateTime(2026, 5, 1, 8, 0, 0, DateTimeKind.Utc),
            UpdatedAt = updatedAt,
            IsActive = true,
        };
        await SeedProductsAsync(database, seeded);
        using var context = database.CreateContext();
        var service = new ProductService(context, NullLogger<ProductService>.Instance);

        var products = await service.GetActiveAsync();

        var product = Assert.Single(products);
        Assert.Equal(seeded.Id, product.Id);
        Assert.Equal(ValidBarcode, product.Code);
        Assert.Equal("Leche entera 1 L", product.Name);
        Assert.Equal(1250, product.SalePrice);
        Assert.Equal(updatedAt, product.UpdatedAt);
        Assert.Equal(DateTimeKind.Utc, product.UpdatedAt.Kind);
    }

    private static Product NewProduct(string code, bool isActive = true) =>
        new() { Name = $"Producto {code}", Code = code, IsActive = isActive };

    private static async Task SeedProductsAsync(TestDatabase database, params Product[] products)
    {
        using var context = database.CreateContext();
        context.Products.AddRange(products);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static SaveProductRequest NewRequest(string code, string name = "Arroz 1 kg", int salePrice = 1250) =>
        new() { Code = code, Name = name, SalePrice = salePrice };

    private static async Task<ProductResponse> CreateProductAsync(TestDatabase database, SaveProductRequest request)
    {
        using var context = database.CreateContext();
        var service = new ProductService(context, NullLogger<ProductService>.Instance);
        return await service.CreateAsync(request);
    }

    private static async Task<Product> FindProductAsync(TestDatabase database, int id)
    {
        using var context = database.CreateContext();
        return await context.Products.SingleAsync(
            product => product.Id == id,
            TestContext.Current.CancellationToken);
    }
}
