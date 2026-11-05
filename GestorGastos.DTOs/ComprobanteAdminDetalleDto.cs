using GestorGastos.DTOs.Enums;

namespace GestorGastos.DTOs
{
    public class ComprobanteAdminDetalleDto
    {
        public int Id { get; set; }
        public string NombreArchivoOriginal { get; set; } = string.Empty;
        public string BlobNombre { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long TamanioBytes { get; set; }
        public TipoDocumento TipoDocumento { get; set; }
        public Categoria Categoria { get; set; }
        public string? Comercio { get; set; }
        public DateOnly? FechaComprobante { get; set; }
        public string? Descripcion { get; set; }
        public decimal? Monto { get; set; }
        public string? Moneda { get; set; }
        public double? ConfianzaDescripcion { get; set; }
        public double? ConfianzaMonto { get; set; }
        public bool EditadoManualmente { get; set; }
        public EstadoComprobante Estado { get; set; }
        public string? MensajeError { get; set; }
        public string? MotivoRechazo { get; set; }
        public DateTime FechaCarga { get; set; }
        public DateTime? FechaPago { get; set; }
        public string EmpleadoNombre { get; set; } = string.Empty;
        public string EmpleadoEmail { get; set; } = string.Empty;
        public string EmpleadoId { get; set; } = string.Empty;
        public List<HistorialEstadoDto> Historial { get; set; } = new List<HistorialEstadoDto>();
    }
}
