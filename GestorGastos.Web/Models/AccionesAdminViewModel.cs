using GestorGastos.DTOs.Enums;

namespace GestorGastos.Web.Models
{
    public class AccionesAdminViewModel
    {
        public int Id { get; set; }
        public EstadoComprobante Estado { get; set; }
        public DateTime FechaCarga { get; set; }
        public bool EnTabla { get; set; }
    }
}
