using System.Text.Json.Serialization;
using GestorGastos.DTOs.Enums;

namespace GestorGastos.DTOs
{
    public class EstadoComprobanteDto
    {
        public int Id { get; set; }
        public string NombreArchivoOriginal { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public EstadoComprobante Estado { get; set; }

        public string EstadoTexto { get; set; } = string.Empty;
        public string ClaseBadge { get; set; } = string.Empty;

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public TipoDocumento TipoDocumento { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public Categoria Categoria { get; set; }

        public string? Comercio { get; set; }
        public DateOnly? FechaComprobante { get; set; }
        public string? Descripcion { get; set; }
        public decimal? Monto { get; set; }
        public string MontoTexto { get; set; } = string.Empty;

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public Moneda? Moneda { get; set; }

        public double? ConfianzaDescripcion { get; set; }
        public double? ConfianzaMonto { get; set; }
        public string? MensajeError { get; set; }
        public bool Editable { get; set; }
    }
}
