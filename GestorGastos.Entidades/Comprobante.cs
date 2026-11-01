using GestorGastos.DTOs.Enums;

namespace GestorGastos.Entidades
{
    public class Comprobante
    {
        public int Id { get; set; }
        public string UsuarioId { get; set; } = string.Empty;
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
        public Moneda? Moneda { get; set; }
        public double? ConfianzaDescripcion { get; set; }
        public double? ConfianzaMonto { get; set; }
        public bool EditadoManualmente { get; set; }
        public EstadoComprobante Estado { get; set; }
        public string? MensajeError { get; set; }
        public string? MotivoRechazo { get; set; }
        public DateTime FechaCarga { get; set; }
        public DateTime? FechaPago { get; set; }

        public Usuario Usuario { get; set; } = null!;
        public List<HistorialEstado> Historial { get; set; } = new List<HistorialEstado>();
    }
}
