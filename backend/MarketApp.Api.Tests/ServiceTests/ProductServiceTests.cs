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
    private const string ProductNotFoundMessage = "El producto no existe.";
    private const string InvalidBarcodeMessage =
        "El código de barras no es válido. Revise que esté bien escrito o marque \"Sin código de barras\".";

    // EAN-13 4006381333931: 4+0+0+18+3+24+1+9+3+9+9+9 = 89 -> check 1 (valid).
    private const string ValidBarcode = "4006381333931";

    // Same code with the last digit changed: expected check 1, not 2.
    private const string BarcodeWithWrongCheckDigit = "4006381333932";

    // EAN-8 96385074: 27+6+9+8+15+0+21 = 86 -> check 4 (valid). The code of the product being updated.
    private const string OriginalCode = "96385074";
    private const string OriginalName = "Producto original";
    private const int OriginalSalePrice = 900;
    private const int OriginalMinStock = 5;

    private static readonly DateTime OriginalCreatedAt = new(2026, 1, 10, 8, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime OriginalUpdatedAt = new(2026, 2, 20, 8, 0, 0, DateTimeKind.Utc);

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

    [Theory]
    [InlineData("  4006381333931", ValidBarcode)]
    [InlineData("4006381333931  ", ValidBarcode)]
    [InlineData("  4006381333931  ", ValidBarcode)]
    [InlineData("tmv00002", "TMV00002")]
    [InlineData("Tmv00002", "TMV00002")]
    [InlineData("  tmv00002  ", "TMV00002")]
    public async Task Create_saves_the_code_without_outer_spaces_and_in_uppercase(string code, string expected)
    {
        using var database = new TestDatabase();
        await SeedProductsAsync(database, NewProduct("TMV00001"));

        var response = await CreateProductAsync(database, NewRequest(code));

        var product = await FindProductAsync(database, response.Id);
        Assert.Equal(expected, product.Code);
    }

    [Theory]
    [InlineData("  Leche entera 1 L")]
    [InlineData("Leche entera 1 L  ")]
    [InlineData("  Leche entera 1 L  ")]
    public async Task Create_saves_the_name_without_outer_spaces(string name)
    {
        using var database = new TestDatabase();

        var response = await CreateProductAsync(database, NewRequest(ValidBarcode, name));

        var product = await FindProductAsync(database, response.Id);
        Assert.Equal("Leche entera 1 L", product.Name);
    }

    [Theory]
    [InlineData("leche ENTERA 1 l")]
    [InlineData("Leche   entera  1 L")]
    public async Task Create_does_not_change_the_case_or_the_inner_spaces_of_the_name(string name)
    {
        using var database = new TestDatabase();

        var response = await CreateProductAsync(database, NewRequest(ValidBarcode, name));

        var product = await FindProductAsync(database, response.Id);
        Assert.Equal(name, product.Name);
    }

    [Fact]
    public async Task Create_returns_the_normalized_code_and_name()
    {
        using var database = new TestDatabase();
        await SeedProductsAsync(database, NewProduct("TMV00001"));

        var response = await CreateProductAsync(database, NewRequest("  tmv00002  ", "  Leche entera 1 L  "));

        Assert.Equal("TMV00002", response.Code);
        Assert.Equal("Leche entera 1 L", response.Name);
    }

    [Theory]
    [InlineData("tmv00001")]
    [InlineData("  tmv00001  ")]
    [InlineData("  4006381333931  ")]
    public async Task Create_rejects_a_code_that_is_a_duplicate_once_normalized(string code)
    {
        using var database = new TestDatabase();
        await SeedProductsAsync(database, NewProduct("TMV00001"), NewProduct(ValidBarcode));

        var exception = await Assert.ThrowsAsync<FieldValidationException>(
            () => CreateProductAsync(database, NewRequest(code)));

        Assert.Equal("Code", exception.Field);
        Assert.Equal(DuplicateCodeMessage, exception.Message);
    }

    [Fact]
    public async Task Create_rejects_an_internal_code_in_lowercase_that_is_not_the_next_one()
    {
        using var database = new TestDatabase();
        await SeedProductsAsync(database, NewProduct("TMV00001"));

        var exception = await Assert.ThrowsAsync<FieldValidationException>(
            () => CreateProductAsync(database, NewRequest("tmv00007")));

        Assert.Equal("Code", exception.Field);
        Assert.Equal(InternalCodeMessage, exception.Message);
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

    [Fact]
    public async Task Orders_the_products_from_the_latest_to_the_earliest_update_regardless_of_the_creation_date()
    {
        using var database = new TestDatabase();
        await SeedProductsAsync(
            database,
            NewProductWithDates(
                "TMV00001",
                createdAt: new DateTime(2026, 1, 10, 8, 0, 0, DateTimeKind.Utc),
                updatedAt: new DateTime(2026, 3, 2, 8, 0, 0, DateTimeKind.Utc)),
            NewProductWithDates(
                "TMV00002",
                createdAt: new DateTime(2026, 1, 20, 8, 0, 0, DateTimeKind.Utc),
                updatedAt: new DateTime(2026, 3, 3, 8, 0, 0, DateTimeKind.Utc)),
            NewProductWithDates(
                "TMV00003",
                createdAt: new DateTime(2026, 1, 30, 8, 0, 0, DateTimeKind.Utc),
                updatedAt: new DateTime(2026, 3, 1, 8, 0, 0, DateTimeKind.Utc)));
        using var context = database.CreateContext();
        var service = new ProductService(context, NullLogger<ProductService>.Instance);

        var products = await service.GetActiveAsync();

        var codes = products.Select(product => product.Code).ToList();
        Assert.Equal(["TMV00002", "TMV00001", "TMV00003"], codes);
    }

    [Fact]
    public async Task Puts_first_the_latest_update_when_the_dates_differ_by_one_millisecond()
    {
        using var database = new TestDatabase();
        var createdAt = new DateTime(2026, 5, 1, 8, 0, 0, DateTimeKind.Utc);
        var earlierUpdate = new DateTime(2026, 5, 15, 9, 30, 0, DateTimeKind.Utc);
        await SeedProductsAsync(
            database,
            NewProductWithDates("TMV00001", createdAt, earlierUpdate),
            NewProductWithDates("TMV00002", createdAt, earlierUpdate.AddMilliseconds(1)));
        using var context = database.CreateContext();
        var service = new ProductService(context, NullLogger<ProductService>.Instance);

        var products = await service.GetActiveAsync();

        var codes = products.Select(product => product.Code).ToList();
        Assert.Equal(["TMV00002", "TMV00001"], codes);
    }

    [Fact]
    public async Task Leaves_out_an_inactive_product_with_the_latest_update_and_keeps_the_order_of_the_rest()
    {
        using var database = new TestDatabase();
        var createdAt = new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc);
        await SeedProductsAsync(
            database,
            NewProductWithDates("TMV00001", createdAt, new DateTime(2026, 3, 2, 8, 0, 0, DateTimeKind.Utc)),
            NewProductWithDates("TMV00002", createdAt, new DateTime(2026, 3, 4, 8, 0, 0, DateTimeKind.Utc), isActive: false),
            NewProductWithDates("TMV00003", createdAt, new DateTime(2026, 3, 1, 8, 0, 0, DateTimeKind.Utc)),
            NewProductWithDates("TMV00004", createdAt, new DateTime(2026, 3, 3, 8, 0, 0, DateTimeKind.Utc)));
        using var context = database.CreateContext();
        var service = new ProductService(context, NullLogger<ProductService>.Instance);

        var products = await service.GetActiveAsync();

        var codes = products.Select(product => product.Code).ToList();
        Assert.Equal(["TMV00004", "TMV00001", "TMV00003"], codes);
    }

    // ---------- GetByIdAsync ----------

    [Fact]
    public async Task Returns_the_requested_product_when_there_are_several()
    {
        using var database = new TestDatabase();
        var requested = NewProduct("TMV00002");
        await SeedProductsAsync(database, NewProduct("TMV00001"), requested, NewProduct("TMV00003"));
        using var context = database.CreateContext();
        var service = new ProductService(context, NullLogger<ProductService>.Instance);

        var product = await service.GetByIdAsync(requested.Id);

        Assert.Equal(requested.Id, product.Id);
        Assert.Equal("TMV00002", product.Code);
    }

    [Fact]
    public async Task Returns_every_field_of_the_requested_product()
    {
        using var database = new TestDatabase();
        var updatedAt = new DateTime(2026, 5, 15, 9, 30, 0, DateTimeKind.Utc);
        var requested = new Product
        {
            Code = ValidBarcode,
            Name = "Leche entera 1 L",
            SalePrice = 1250,
            CreatedAt = new DateTime(2026, 5, 1, 8, 0, 0, DateTimeKind.Utc),
            UpdatedAt = updatedAt,
            IsActive = true,
        };
        await SeedProductsAsync(database, NewProduct("TMV00001"), requested);
        using var context = database.CreateContext();
        var service = new ProductService(context, NullLogger<ProductService>.Instance);

        var product = await service.GetByIdAsync(requested.Id);

        Assert.Equal(requested.Id, product.Id);
        Assert.Equal(ValidBarcode, product.Code);
        Assert.Equal("Leche entera 1 L", product.Name);
        Assert.Equal(1250, product.SalePrice);
        Assert.Equal(updatedAt, product.UpdatedAt);
    }

    [Fact]
    public async Task Returns_the_update_date_of_the_requested_product_in_utc()
    {
        using var database = new TestDatabase();
        var requested = NewProductWithDates(
            "TMV00001",
            createdAt: new DateTime(2026, 5, 1, 8, 0, 0, DateTimeKind.Utc),
            updatedAt: new DateTime(2026, 5, 15, 9, 30, 0, DateTimeKind.Utc));
        await SeedProductsAsync(database, requested);
        using var context = database.CreateContext();
        var service = new ProductService(context, NullLogger<ProductService>.Instance);

        var product = await service.GetByIdAsync(requested.Id);

        Assert.Equal(DateTimeKind.Utc, product.UpdatedAt.Kind);
    }

    [Fact]
    public async Task Returns_an_inactive_product()
    {
        using var database = new TestDatabase();
        var requested = NewProduct("TMV00001", isActive: false);
        await SeedProductsAsync(database, requested);
        using var context = database.CreateContext();
        var service = new ProductService(context, NullLogger<ProductService>.Instance);

        var product = await service.GetByIdAsync(requested.Id);

        Assert.Equal(requested.Id, product.Id);
        Assert.Equal("TMV00001", product.Code);
    }

    [Fact]
    public async Task Throws_not_found_when_the_id_belongs_to_no_product_and_others_exist()
    {
        using var database = new TestDatabase();
        var existing = NewProduct("TMV00001");
        await SeedProductsAsync(database, existing);
        using var context = database.CreateContext();
        var service = new ProductService(context, NullLogger<ProductService>.Instance);

        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => service.GetByIdAsync(existing.Id + 1));

        Assert.Equal(ProductNotFoundMessage, exception.Message);
    }

    [Fact]
    public async Task Throws_not_found_when_there_are_no_products()
    {
        using var database = new TestDatabase();
        using var context = database.CreateContext();
        var service = new ProductService(context, NullLogger<ProductService>.Instance);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(1));

        Assert.Equal(ProductNotFoundMessage, exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Throws_not_found_when_the_id_is_not_positive(int id)
    {
        using var database = new TestDatabase();
        await SeedProductsAsync(database, NewProduct("TMV00001"));
        using var context = database.CreateContext();
        var service = new ProductService(context, NullLogger<ProductService>.Instance);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(id));

        Assert.Equal(ProductNotFoundMessage, exception.Message);
    }

    // ---------- UpdateAsync ----------

    [Fact]
    public async Task Update_saves_and_returns_the_new_data_with_the_current_update_date()
    {
        using var database = new TestDatabase();
        var original = NewProductToUpdate();
        await SeedProductsAsync(database, original);

        var before = DateTime.UtcNow;
        var response = await UpdateProductAsync(
            database, original.Id, NewRequest(ValidBarcode, "Leche entera 1 L", 1400));
        var after = DateTime.UtcNow;

        Assert.Equal(original.Id, response.Id);
        Assert.Equal(ValidBarcode, response.Code);
        Assert.Equal("Leche entera 1 L", response.Name);
        Assert.Equal(1400, response.SalePrice);
        Assert.InRange(response.UpdatedAt, before, after);

        var product = await FindProductAsync(database, original.Id);
        Assert.Equal(ValidBarcode, product.Code);
        Assert.Equal("Leche entera 1 L", product.Name);
        Assert.Equal(1400, product.SalePrice);
        Assert.InRange(product.UpdatedAt, before, after);
    }

    [Fact]
    public async Task Update_does_not_change_the_fields_outside_the_request()
    {
        using var database = new TestDatabase();
        var original = NewProductToUpdate();
        await SeedProductsAsync(database, original);

        await UpdateProductAsync(database, original.Id, NewRequest(ValidBarcode));

        var product = await FindProductAsync(database, original.Id);
        Assert.Equal(OriginalCreatedAt, product.CreatedAt);
        Assert.Equal(OriginalMinStock, product.MinStock);
        Assert.True(product.IsActive);
    }

    [Fact]
    public async Task Update_does_not_change_other_products()
    {
        using var database = new TestDatabase();
        var original = NewProductToUpdate();
        var other = NewProductWithDates("TMV00001", OriginalCreatedAt, OriginalUpdatedAt);
        await SeedProductsAsync(database, original, other);

        await UpdateProductAsync(database, original.Id, NewRequest(ValidBarcode, "Leche entera 1 L", 1400));

        var product = await FindProductAsync(database, other.Id);
        Assert.Equal("TMV00001", product.Code);
        Assert.Equal("Producto TMV00001", product.Name);
        Assert.Equal(other.SalePrice, product.SalePrice);
        Assert.Equal(OriginalUpdatedAt, product.UpdatedAt);
    }

    [Fact]
    public async Task Update_accepts_the_next_internal_code()
    {
        using var database = new TestDatabase();
        var original = NewProductToUpdate();
        await SeedProductsAsync(database, original, NewProduct("TMV00001"));

        await UpdateProductAsync(database, original.Id, NewRequest("TMV00002"));

        var product = await FindProductAsync(database, original.Id);
        Assert.Equal("TMV00002", product.Code);
    }

    [Fact]
    public async Task Update_rejects_an_invalid_barcode()
    {
        using var database = new TestDatabase();
        var original = NewProductToUpdate();
        await SeedProductsAsync(database, original);

        var exception = await Assert.ThrowsAsync<FieldValidationException>(
            () => UpdateProductAsync(database, original.Id, NewRequest(BarcodeWithWrongCheckDigit)));

        Assert.Equal("Code", exception.Field);
        Assert.Equal(InvalidBarcodeMessage, exception.Message);
    }

    [Fact]
    public async Task Update_rejects_an_internal_code_that_is_not_the_next_one()
    {
        using var database = new TestDatabase();
        var original = NewProductToUpdate();
        await SeedProductsAsync(database, original, NewProduct("TMV00001"));

        var exception = await Assert.ThrowsAsync<FieldValidationException>(
            () => UpdateProductAsync(database, original.Id, NewRequest("TMV00007")));

        Assert.Equal("Code", exception.Field);
        Assert.Equal(InternalCodeMessage, exception.Message);
    }

    [Fact]
    public async Task Update_rejects_a_code_that_another_product_already_has()
    {
        using var database = new TestDatabase();
        var original = NewProductToUpdate();
        await SeedProductsAsync(database, original, NewProduct(ValidBarcode));

        var exception = await Assert.ThrowsAsync<FieldValidationException>(
            () => UpdateProductAsync(database, original.Id, NewRequest(ValidBarcode)));

        Assert.Equal("Code", exception.Field);
        Assert.Equal(DuplicateCodeMessage, exception.Message);
    }

    [Fact]
    public async Task Update_rejects_a_code_that_an_inactive_product_already_has()
    {
        using var database = new TestDatabase();
        var original = NewProductToUpdate();
        await SeedProductsAsync(database, original, NewProduct(ValidBarcode, isActive: false));

        var exception = await Assert.ThrowsAsync<FieldValidationException>(
            () => UpdateProductAsync(database, original.Id, NewRequest(ValidBarcode)));

        Assert.Equal("Code", exception.Field);
        Assert.Equal(DuplicateCodeMessage, exception.Message);
    }

    [Fact]
    public async Task Update_reports_an_internal_code_of_another_product_as_a_duplicate()
    {
        using var database = new TestDatabase();
        var original = NewProductToUpdate();
        await SeedProductsAsync(database, original, NewProduct("TMV00001"));

        var exception = await Assert.ThrowsAsync<FieldValidationException>(
            () => UpdateProductAsync(database, original.Id, NewRequest("TMV00001")));

        Assert.Equal("Code", exception.Field);
        Assert.Equal(DuplicateCodeMessage, exception.Message);
    }

    // Rejected codes: bad check digit, wrong internal code, duplicate barcode, duplicate internal code.
    [Theory]
    [InlineData(BarcodeWithWrongCheckDigit)]
    [InlineData("TMV00007")]
    [InlineData(ValidBarcode)]
    [InlineData("TMV00001")]
    public async Task Update_does_not_save_changes_that_fail_validation(string code)
    {
        using var database = new TestDatabase();
        var original = NewProductToUpdate();
        await SeedProductsAsync(database, original, NewProduct("TMV00001"), NewProduct(ValidBarcode));

        await Assert.ThrowsAsync<FieldValidationException>(
            () => UpdateProductAsync(database, original.Id, NewRequest(code, "Leche entera 1 L", 1400)));

        var product = await FindProductAsync(database, original.Id);
        Assert.Equal(OriginalCode, product.Code);
        Assert.Equal(OriginalName, product.Name);
        Assert.Equal(OriginalSalePrice, product.SalePrice);
        Assert.Equal(OriginalUpdatedAt, product.UpdatedAt);
    }

    [Fact]
    public async Task Update_throws_not_found_when_the_id_belongs_to_no_product_and_others_exist()
    {
        using var database = new TestDatabase();
        var existing = NewProductToUpdate();
        await SeedProductsAsync(database, existing);

        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => UpdateProductAsync(database, existing.Id + 1, NewRequest(ValidBarcode)));

        Assert.Equal(ProductNotFoundMessage, exception.Message);
    }

    [Fact]
    public async Task Update_throws_not_found_when_there_are_no_products()
    {
        using var database = new TestDatabase();

        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => UpdateProductAsync(database, 1, NewRequest(ValidBarcode)));

        Assert.Equal(ProductNotFoundMessage, exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Update_throws_not_found_when_the_id_is_not_positive(int id)
    {
        using var database = new TestDatabase();
        await SeedProductsAsync(database, NewProductToUpdate());

        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => UpdateProductAsync(database, id, NewRequest(ValidBarcode)));

        Assert.Equal(ProductNotFoundMessage, exception.Message);
    }

    [Fact]
    public async Task Update_does_not_save_anything_when_the_product_does_not_exist()
    {
        using var database = new TestDatabase();
        var existing = NewProductToUpdate();
        await SeedProductsAsync(database, existing);

        await Assert.ThrowsAsync<NotFoundException>(
            () => UpdateProductAsync(database, existing.Id + 1, NewRequest(ValidBarcode, "Leche entera 1 L", 1400)));

        using var verification = database.CreateContext();
        var product = await verification.Products.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(OriginalCode, product.Code);
        Assert.Equal(OriginalName, product.Name);
        Assert.Equal(OriginalSalePrice, product.SalePrice);
    }

    [Fact]
    public async Task Update_accepts_the_barcode_the_product_already_has()
    {
        using var database = new TestDatabase();
        var original = NewProductToUpdate();
        await SeedProductsAsync(database, original);

        await UpdateProductAsync(database, original.Id, NewRequest(OriginalCode, "Leche entera 1 L", 1400));

        var product = await FindProductAsync(database, original.Id);
        Assert.Equal(OriginalCode, product.Code);
        Assert.Equal("Leche entera 1 L", product.Name);
        Assert.Equal(1400, product.SalePrice);
    }

    [Theory]
    [InlineData("TMV00001")]
    [InlineData("TMV00002")]
    public async Task Update_accepts_the_internal_code_the_product_already_has(string code)
    {
        using var database = new TestDatabase();
        var seeded = new[] { NewProduct("TMV00001"), NewProduct("TMV00002") };
        await SeedProductsAsync(database, seeded);
        var original = seeded.Single(product => product.Code == code);

        await UpdateProductAsync(database, original.Id, NewRequest(code, "Leche entera 1 L", 1400));

        var updated = await FindProductAsync(database, original.Id);
        Assert.Equal(code, updated.Code);
        Assert.Equal("Leche entera 1 L", updated.Name);
        Assert.Equal(1400, updated.SalePrice);
    }

    [Fact]
    public async Task Update_accepts_changing_an_internal_code_to_the_next_one()
    {
        using var database = new TestDatabase();
        var original = NewProduct("TMV00001");
        await SeedProductsAsync(database, original);

        await UpdateProductAsync(database, original.Id, NewRequest("TMV00002"));

        var product = await FindProductAsync(database, original.Id);
        Assert.Equal("TMV00002", product.Code);
    }

    [Fact]
    public async Task Update_saves_the_changes_of_an_inactive_product()
    {
        using var database = new TestDatabase();
        var original = NewProductWithDates(OriginalCode, OriginalCreatedAt, OriginalUpdatedAt, isActive: false);
        await SeedProductsAsync(database, original);

        await UpdateProductAsync(database, original.Id, NewRequest(ValidBarcode, "Leche entera 1 L", 1400));

        var product = await FindProductAsync(database, original.Id);
        Assert.Equal(ValidBarcode, product.Code);
        Assert.Equal("Leche entera 1 L", product.Name);
        Assert.Equal(1400, product.SalePrice);
    }

    [Fact]
    public async Task Update_keeps_an_inactive_product_inactive()
    {
        using var database = new TestDatabase();
        var original = NewProductWithDates(OriginalCode, OriginalCreatedAt, OriginalUpdatedAt, isActive: false);
        await SeedProductsAsync(database, original);

        await UpdateProductAsync(database, original.Id, NewRequest(ValidBarcode));

        var product = await FindProductAsync(database, original.Id);
        Assert.False(product.IsActive);
    }

    // Each code would fail validation by itself: bad check digit, wrong internal code, duplicate barcode.
    [Theory]
    [InlineData(BarcodeWithWrongCheckDigit)]
    [InlineData("TMV00007")]
    [InlineData(ValidBarcode)]
    public async Task Update_reports_not_found_before_an_invalid_code(string code)
    {
        using var database = new TestDatabase();
        var existing = NewProduct(ValidBarcode);
        await SeedProductsAsync(database, existing);

        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => UpdateProductAsync(database, existing.Id + 1, NewRequest(code)));

        Assert.Equal(ProductNotFoundMessage, exception.Message);
    }

    [Fact]
    public async Task Update_leaves_updated_by_empty()
    {
        using var database = new TestDatabase();
        var original = NewProductToUpdate();
        await SeedProductsAsync(database, original);

        await UpdateProductAsync(database, original.Id, NewRequest(ValidBarcode));

        var product = await FindProductAsync(database, original.Id);
        Assert.Null(product.UpdatedById);
    }

    [Fact]
    public async Task Update_returns_the_update_date_in_utc()
    {
        using var database = new TestDatabase();
        var original = NewProductToUpdate();
        await SeedProductsAsync(database, original);

        var response = await UpdateProductAsync(database, original.Id, NewRequest(ValidBarcode));

        Assert.Equal(DateTimeKind.Utc, response.UpdatedAt.Kind);
    }

    [Theory]
    [InlineData("  4006381333931", ValidBarcode)]
    [InlineData("4006381333931  ", ValidBarcode)]
    [InlineData("  4006381333931  ", ValidBarcode)]
    [InlineData("tmv00002", "TMV00002")]
    [InlineData("Tmv00002", "TMV00002")]
    [InlineData("  tmv00002  ", "TMV00002")]
    public async Task Update_saves_the_code_without_outer_spaces_and_in_uppercase(string code, string expected)
    {
        using var database = new TestDatabase();
        var original = NewProductToUpdate();
        await SeedProductsAsync(database, original, NewProduct("TMV00001"));

        await UpdateProductAsync(database, original.Id, NewRequest(code));

        var product = await FindProductAsync(database, original.Id);
        Assert.Equal(expected, product.Code);
    }

    [Theory]
    [InlineData("  Leche entera 1 L")]
    [InlineData("Leche entera 1 L  ")]
    [InlineData("  Leche entera 1 L  ")]
    public async Task Update_saves_the_name_without_outer_spaces(string name)
    {
        using var database = new TestDatabase();
        var original = NewProductToUpdate();
        await SeedProductsAsync(database, original);

        await UpdateProductAsync(database, original.Id, NewRequest(ValidBarcode, name));

        var product = await FindProductAsync(database, original.Id);
        Assert.Equal("Leche entera 1 L", product.Name);
    }

    [Theory]
    [InlineData("leche ENTERA 1 l")]
    [InlineData("Leche   entera  1 L")]
    public async Task Update_does_not_change_the_case_or_the_inner_spaces_of_the_name(string name)
    {
        using var database = new TestDatabase();
        var original = NewProductToUpdate();
        await SeedProductsAsync(database, original);

        await UpdateProductAsync(database, original.Id, NewRequest(ValidBarcode, name));

        var product = await FindProductAsync(database, original.Id);
        Assert.Equal(name, product.Name);
    }

    [Fact]
    public async Task Update_returns_the_normalized_code_and_name()
    {
        using var database = new TestDatabase();
        var original = NewProductToUpdate();
        await SeedProductsAsync(database, original, NewProduct("TMV00001"));

        var response = await UpdateProductAsync(
            database, original.Id, NewRequest("  tmv00002  ", "  Leche entera 1 L  "));

        Assert.Equal("TMV00002", response.Code);
        Assert.Equal("Leche entera 1 L", response.Name);
    }

    [Theory]
    [InlineData("tmv00001")]
    [InlineData("  tmv00001  ")]
    [InlineData("  4006381333931  ")]
    public async Task Update_rejects_a_code_that_is_a_duplicate_once_normalized(string code)
    {
        using var database = new TestDatabase();
        var original = NewProductToUpdate();
        await SeedProductsAsync(database, original, NewProduct("TMV00001"), NewProduct(ValidBarcode));

        var exception = await Assert.ThrowsAsync<FieldValidationException>(
            () => UpdateProductAsync(database, original.Id, NewRequest(code)));

        Assert.Equal("Code", exception.Field);
        Assert.Equal(DuplicateCodeMessage, exception.Message);
    }

    [Fact]
    public async Task Update_rejects_an_internal_code_in_lowercase_that_is_not_the_next_one()
    {
        using var database = new TestDatabase();
        var original = NewProductToUpdate();
        await SeedProductsAsync(database, original, NewProduct("TMV00001"));

        var exception = await Assert.ThrowsAsync<FieldValidationException>(
            () => UpdateProductAsync(database, original.Id, NewRequest("tmv00007")));

        Assert.Equal("Code", exception.Field);
        Assert.Equal(InternalCodeMessage, exception.Message);
    }

    [Theory]
    [InlineData("TMV00001", "  tmv00001  ")]
    [InlineData(OriginalCode, "  96385074  ")]
    public async Task Update_accepts_the_code_the_product_already_has_once_normalized(string storedCode, string code)
    {
        using var database = new TestDatabase();
        var original = NewProduct(storedCode);
        await SeedProductsAsync(database, original, NewProduct("TMV00002"));

        await UpdateProductAsync(database, original.Id, NewRequest(code));

        var product = await FindProductAsync(database, original.Id);
        Assert.Equal(storedCode, product.Code);
    }

    private static Product NewProductToUpdate() =>
        new()
        {
            Code = OriginalCode,
            Name = OriginalName,
            SalePrice = OriginalSalePrice,
            MinStock = OriginalMinStock,
            CreatedAt = OriginalCreatedAt,
            UpdatedAt = OriginalUpdatedAt,
            IsActive = true,
        };

    private static async Task<ProductResponse> UpdateProductAsync(
        TestDatabase database, int id, SaveProductRequest request)
    {
        using var context = database.CreateContext();
        var service = new ProductService(context, NullLogger<ProductService>.Instance);
        return await service.UpdateAsync(id, request);
    }

    private static Product NewProductWithDates(
        string code, DateTime createdAt, DateTime updatedAt, bool isActive = true) =>
        new()
        {
            Name = $"Producto {code}",
            Code = code,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            IsActive = isActive,
        };

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
