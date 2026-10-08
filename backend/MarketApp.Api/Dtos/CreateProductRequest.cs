using System.ComponentModel.DataAnnotations;

namespace MarketApp.Api.Dtos;

public record CreateProductRequest
{
    [Required(ErrorMessage = "Ingrese el código del producto.")]
    public string? Code { get; init; }

    [Required(ErrorMessage = "Ingrese el nombre del producto.")]
    public string? Name { get; init; }

    [Required(ErrorMessage = "Ingrese el precio de venta.")]
    [Range(1, int.MaxValue, ErrorMessage = "El precio de venta debe ser mayor a cero.")]
    public int? SalePrice { get; init; }
}
