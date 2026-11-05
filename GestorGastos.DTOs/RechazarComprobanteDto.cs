using System.ComponentModel.DataAnnotations;

namespace GestorGastos.DTOs
{
    public class RechazarComprobanteDto
    {
        [Required(ErrorMessage = "Escribí un motivo.")]
        [StringLength(500, MinimumLength = 10, ErrorMessage = "Escribí un motivo de entre 10 y 500 caracteres.")]
        public string Motivo { get; set; } = string.Empty;
    }
}
