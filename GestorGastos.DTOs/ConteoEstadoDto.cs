using GestorGastos.DTOs.Enums;

namespace GestorGastos.DTOs
{
    public class ConteoEstadoDto
    {
        public EstadoComprobante Estado { get; set; }
        public int Cantidad { get; set; }
    }
}
