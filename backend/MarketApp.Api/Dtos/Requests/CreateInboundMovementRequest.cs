using System.ComponentModel.DataAnnotations;

namespace MarketApp.Api.Dtos.Requests;

public record CreateInboundMovementRequest
{
    [Required(ErrorMessage = "Elija el producto.")]
    public int? ProductId { get; init; }

    [Required(ErrorMessage = "Ingrese la cantidad de unidades del lote.")]
    [Range(1, 9999, ErrorMessage = "La cantidad debe estar entre 1 y 9.999.")]
    public int? Quantity { get; init; }

    [Required(ErrorMessage = "Ingrese el costo total del lote.")]
    [Range(0, 9999999, ErrorMessage = "El costo total debe estar entre Bs 0,00 y Bs 99.999,99.")]
    public int? TotalCost { get; init; }

    [Required(ErrorMessage = "Ingrese la fecha de ingreso.")]
    public DateOnly? MovementDate { get; init; }

    [MaxLength(500, ErrorMessage = "La nota no puede tener más de 500 caracteres.")]
    public string? Note { get; init; }
}
