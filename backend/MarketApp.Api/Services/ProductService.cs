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
    private const string ProductNotFoundMessage = "El producto no existe.";

    public async Task<ProductResponse> CreateAsync(SaveProductRequest request)
    {
        var code = NormalizeCode(request.Code!);
        await ValidateNewCodeAsync(code);

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

        return new ProductResponse(product.Id, product.Code, product.Name, product.SalePrice, product.UpdatedAt);
    }

    public async Task<ProductResponse> UpdateAsync(int id, SaveProductRequest request)
    {
        var product = await db.Products.FindAsync(id) ?? throw new NotFoundException(ProductNotFoundMessage);

        var code = NormalizeCode(request.Code!);
        if (code != product.Code)
        {
            await ValidateNewCodeAsync(code);
        }

        var (oldCode, oldName, oldSalePrice) = (product.Code, product.Name, product.SalePrice);
        product.Code = code;
        product.Name = request.Name!.Trim();
        product.SalePrice = request.SalePrice!.Value;
        product.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        logger.LogInformation(
            "Product {ProductId} updated: {OldCode} {OldName} {OldSalePrice} -> {Code} {Name} {SalePrice}",
            product.Id, oldCode, oldName, oldSalePrice, product.Code, product.Name, product.SalePrice);

        return new ProductResponse(product.Id, product.Code, product.Name, product.SalePrice, product.UpdatedAt);
    }

    public async Task<List<ProductResponse>> GetActiveAsync()
    {
        return await db.Products
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.UpdatedAt)
            .ThenByDescending(p => p.Id)
            .Select(p => new ProductResponse(p.Id, p.Code, p.Name, p.SalePrice, p.UpdatedAt))
            .ToListAsync();
    }

    public async Task<ProductResponse> GetByIdAsync(int id)
    {
        return await db.Products
            .Where(p => p.Id == id)
            .Select(p => new ProductResponse(p.Id, p.Code, p.Name, p.SalePrice, p.UpdatedAt))
            .FirstOrDefaultAsync() ?? throw new NotFoundException(ProductNotFoundMessage);
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

    private static string NormalizeCode(string code) => code.Trim().ToUpperInvariant();

    private async Task ValidateNewCodeAsync(string code)
    {
        if (await db.Products.AnyAsync(p => p.Code == code))
        {
            throw new FieldValidationException(nameof(SaveProductRequest.Code), "Ya existe un producto con este código.");
        }

        if (code.StartsWith(InternalCodePrefix, StringComparison.Ordinal))
        {
            if (code != await GetNextInternalCodeAsync())
            {
                throw new FieldValidationException(nameof(SaveProductRequest.Code), "Los códigos internos los asigna el sistema.");
            }
        }
        else
        {
            BarcodeHelper.Validate(code, nameof(SaveProductRequest.Code));
        }
    }
}
