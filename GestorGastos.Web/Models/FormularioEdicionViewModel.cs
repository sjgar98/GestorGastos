using GestorGastos.DTOs;

namespace GestorGastos.Web.Models
{
    public class FormularioEdicionViewModel
    {
        public EditarComprobanteDto Formulario { get; set; } = new EditarComprobanteDto();
        public double? ConfianzaDescripcion { get; set; }
        public double? ConfianzaMonto { get; set; }
    }
}
