using GestorGastos.DTOs.Enums;

namespace GestorGastos.DTOs
{
    public class HistorialEstadoDto
    {
        public EstadoComprobante? EstadoAnterior { get; set; }
        public EstadoComprobante EstadoNuevo { get; set; }
        public DateTime Fecha { get; set; }
        public string? UsuarioNombre { get; set; }
        public string? Observacion { get; set; }
    }
}
