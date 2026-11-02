using MarketApp.Api.Dtos.Requests;
using MarketApp.Api.Dtos.Responses;

namespace MarketApp.Api.Services.Interfaces;

public interface IProductService
{
    Task<ProductResponse> CreateAsync(SaveProductRequest request);

    Task<ProductResponse> UpdateAsync(int id, SaveProductRequest request);

    Task<List<ProductResponse>> GetActiveAsync();

    Task<ProductResponse> GetByIdAsync(int id);

    Task<string> GetNextInternalCodeAsync();
}
