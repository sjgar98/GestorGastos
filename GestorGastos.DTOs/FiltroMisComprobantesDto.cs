using GestorGastos.DTOs.Enums;

namespace GestorGastos.DTOs
{
    public class FiltroMisComprobantesDto
    {
        public EstadoComprobante? Estado { get; set; }
        public string? Orden { get; set; } = "recientes";
        public int Pagina { get; set; } = 1;
    }
}
