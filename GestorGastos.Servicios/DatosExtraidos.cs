using GestorGastos.DTOs.Enums;

namespace GestorGastos.Servicios
{
    public class DatosExtraidos
    {
        public string? Comercio { get; set; }
        public DateOnly? FechaComprobante { get; set; }
        public string? Descripcion { get; set; }
        public decimal? Monto { get; set; }
        public Moneda? Moneda { get; set; }
        public double? ConfianzaDescripcion { get; set; }
        public double? ConfianzaMonto { get; set; }
    }
}
