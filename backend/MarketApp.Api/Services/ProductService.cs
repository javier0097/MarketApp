using MarketApp.Api.Data;
using MarketApp.Api.Dtos.Requests;
using MarketApp.Api.Dtos.Responses;
using MarketApp.Api.Entities;
using MarketApp.Api.Exceptions;
using MarketApp.Api.Helpers;
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

        if (code.StartsWith(InternalCodePrefix, StringComparison.Ordinal))
        {
            if (code != await GetNextInternalCodeAsync())
            {
                throw new FieldValidationException(nameof(request.Code), "Los códigos internos los asigna el sistema.");
            }
        }
        else
        {
            BarcodeHelper.Validate(code, nameof(request.Code));
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

        return new ProductResponse(product.Id, product.Code, product.Name, product.SalePrice, product.CreatedAt);
    }

    public async Task<List<ProductResponse>> GetActiveAsync()
    {
        return await db.Products
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.CreatedAt)
            .ThenByDescending(p => p.Id)
            .Select(p => new ProductResponse(p.Id, p.Code, p.Name, p.SalePrice, p.CreatedAt))
            .ToListAsync();
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
