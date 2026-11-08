using GestorGastos.DTOs.Enums;

namespace GestorGastos.DTOs
{
    public class ComprobanteListadoDto
    {
        public int Id { get; set; }
        public string NombreArchivoOriginal { get; set; } = string.Empty;
        public DateTime FechaCarga { get; set; }
        public decimal? Monto { get; set; }
        public string? Moneda { get; set; }
        public EstadoComprobante Estado { get; set; }
    }
}
