using GestorGastos.DTOs;

namespace GestorGastos.Web.Models
{
    public class DetalleComprobanteViewModel
    {
        public ComprobanteDetalleDto Comprobante { get; set; } = null!;
        public EditarComprobanteDto Formulario { get; set; } = new EditarComprobanteDto();
    }
}