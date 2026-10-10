using MarketApp.Api.Data;
using MarketApp.Api.Dtos;
using MarketApp.Api.Entities;
using MarketApp.Api.Exceptions;
using MarketApp.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MarketApp.Api.Services;

public class ProductService(MarketAppDbContext db, ILogger<ProductService> logger) : IProductService
{
    private const string InternalCodePrefix = "TMV";

    public async Task<ProductResponse> CreateAsync(CreateProductRequest request)
    {
        var code = request.Code!.Trim().ToUpperInvariant();
        if (await db.Products.AnyAsync(p => p.Code == code))
        {
            throw new FieldValidationException(nameof(request.Code), "Ya existe un producto con este código.");
        }

        if (code.StartsWith(InternalCodePrefix, StringComparison.Ordinal) && code != await GetNextInternalCodeAsync())
        {
            throw new FieldValidationException(nameof(request.Code), "Los códigos internos los asigna el sistema.");
        }

        var now = DateTime.UtcNow;
        var product = new Product
        {
            Code = code,
            Name = request.Name!.Trim(),
            SalePrice = request.SalePrice!.Value,
            MinStock = 0,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.Products.Add(product);
        await db.SaveChangesAsync();
        logger.LogInformation("Product {ProductId} created: {Code} {Name} {SalePrice}", product.Id, product.Code, product.Name, product.SalePrice);

        return new ProductResponse(product.Id, product.Code, product.Name, product.SalePrice);
    }

    public async Task<string> GetNextInternalCodeAsync()
    {
        var lastCode = await db.Products
            .Where(p => p.Code.StartsWith(InternalCodePrefix))
            .OrderByDescending(p => p.Code)
            .Select(p => p.Code)
            .FirstOrDefaultAsync();

        var lastNumber = lastCode is null ? 0 : int.Parse(lastCode[InternalCodePrefix.Length..]);
        return $"{InternalCodePrefix}{lastNumber + 1:D5}";
    }
}
