using GestorGastos.DTOs.Enums;

namespace GestorGastos.DTOs
{
    public class ComprobanteAdminListadoDto
    {
        public int Id { get; set; }
        public string UsuarioId { get; set; } = string.Empty;
        public string NombreEmpleado { get; set; } = string.Empty;
        public string EmailEmpleado { get; set; } = string.Empty;
        public string NombreArchivoOriginal { get; set; } = string.Empty;
        public DateTime FechaCarga { get; set; }
        public decimal? Monto { get; set; }
        public Moneda? Moneda { get; set; }
        public EstadoComprobante Estado { get; set; }
    }
}