using GestorGastos.DTOs;

namespace GestorGastos.Web.Models
{
    public class ResultadoCargaViewModel
    {
        public EstadoComprobanteDto Comprobante { get; set; } = null!;
        public EditarComprobanteDto Formulario { get; set; } = new EditarComprobanteDto();
    }
}
