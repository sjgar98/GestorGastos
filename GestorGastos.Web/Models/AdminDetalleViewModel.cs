using GestorGastos.DTOs;

namespace GestorGastos.Web.Models
{
    public class AdminDetalleViewModel
    {
        public ComprobanteAdminDetalleDto Comprobante { get; set; } = null!;

        /// <summary>
        /// Indica si este comprobante pertenece al administrador logueado (RN-15).
        /// En ese caso se muestran las acciones de estado deshabilitadas con alerta.
        /// </summary>
        public bool EsPropio { get; set; }
    }
}
