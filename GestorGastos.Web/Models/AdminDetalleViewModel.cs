using GestorGastos.DTOs;

namespace GestorGastos.Web.Models
{
    public class AdminDetalleViewModel
    {
        public ComprobanteAdminDetalleDto Comprobante { get; set; } = null!;

        public bool EsPropio { get; set; }
    }
}
