using System.ComponentModel.DataAnnotations;

namespace GestorGastos.DTOs
{
    public class PagarComprobanteDto
    {
        [Required(ErrorMessage = "Ingresá la fecha de pago.")]
        public DateOnly? FechaPago { get; set; }
    }
}
