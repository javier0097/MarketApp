using MarketApp.Api.Dtos.Requests;
using MarketApp.Api.Dtos.Responses;

namespace MarketApp.Api.Services.Interfaces;

public interface IProductService
{
    Task<ProductResponse> CreateAsync(CreateProductRequest request);

    Task<List<ProductResponse>> GetActiveAsync();

    Task<string> GetNextInternalCodeAsync();
}
