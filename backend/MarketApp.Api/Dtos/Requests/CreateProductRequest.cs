using System.ComponentModel.DataAnnotations;

namespace MarketApp.Api.Dtos.Requests;

public record CreateProductRequest
{
    [Required(ErrorMessage = "Ingrese el código del producto.")]
    public string? Code { get; init; }

    [Required(ErrorMessage = "Ingrese el nombre del producto.")]
    [MaxLength(200, ErrorMessage = "El nombre no puede tener más de 200 caracteres.")]
    public string? Name { get; init; }

    [Required(ErrorMessage = "Ingrese el precio de venta.")]
    [Range(1, 999999, ErrorMessage = "El precio de venta debe estar entre Bs 0,01 y Bs 9.999,99.")]
    public int? SalePrice { get; init; }
}
