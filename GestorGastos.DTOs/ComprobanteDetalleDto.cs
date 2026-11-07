using GestorGastos.DTOs.Enums;

namespace GestorGastos.DTOs
{
    public class ComprobanteDetalleDto
    {
        public int Id { get; set; }
        public string NombreArchivoOriginal { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public TipoDocumento TipoDocumento { get; set; }
        public Categoria Categoria { get; set; }
        public EstadoComprobante Estado { get; set; }
        public string? Comercio { get; set; }
        public DateOnly? FechaComprobante { get; set; }
        public string? Descripcion { get; set; }
        public decimal? Monto { get; set; }
        public Moneda? Moneda { get; set; }
        public string? MensajeError { get; set; }
        public string? MotivoRechazo { get; set; }
        public DateTime FechaCarga { get; set; }
        public DateTime? FechaPago { get; set; }
        public bool EditadoManualmente { get; set; }
        public List<HistorialEstadoDto> Historial { get; set; } = new List<HistorialEstadoDto>();
    }
}