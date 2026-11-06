using GestorGastos.DTOs.Enums;

namespace GestorGastos.DTOs
{
    public class CambioEstadoRespuestaDto
    {
        public int Id { get; set; }
        public EstadoComprobante Estado { get; set; }
        public string EstadoTexto { get; set; } = string.Empty;
    }
}
