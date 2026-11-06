using System.ComponentModel.DataAnnotations;
using GestorGastos.DTOs.Enums;
using GestorGastos.DTOs.Validaciones;

namespace GestorGastos.DTOs
{
    public class EditarComprobanteDto
    {
        private const string MensajeDescripcion = "Escribí una descripción de entre 3 y 500 caracteres.";
        private const string MensajeMonto = "Ingresá un monto mayor a 0, con hasta 2 decimales.";

        [StringLength(200, ErrorMessage = "El comercio no puede superar los 200 caracteres.")]
        public string? Comercio { get; set; }

        [FechaNoFutura]
        public DateOnly? FechaComprobante { get; set; }

        [Required(ErrorMessage = MensajeDescripcion)]
        [StringLength(500, MinimumLength = 3, ErrorMessage = MensajeDescripcion)]
        public string? Descripcion { get; set; }

        [Required(ErrorMessage = MensajeMonto)]
        [Range(typeof(decimal), "0.01", "99999999.99", ErrorMessage = MensajeMonto,
            ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
        [MaximoDosDecimales(ErrorMessage = MensajeMonto)]
        public decimal? Monto { get; set; }

        [Required(ErrorMessage = "Elegí una moneda.")]
        public Moneda? Moneda { get; set; }

        [Required(ErrorMessage = "Elegí una categoría.")]
        public Categoria? Categoria { get; set; }
    }
}
