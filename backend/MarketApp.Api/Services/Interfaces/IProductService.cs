using MarketApp.Api.Dtos;

namespace MarketApp.Api.Services.Interfaces;

public interface IProductService
{
    Task<ProductResponse> CreateAsync(CreateProductRequest request);

    Task<string> GetNextInternalCodeAsync();
}
