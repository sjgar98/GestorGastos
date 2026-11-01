using GestorGastos.DTOs.Enums;

namespace GestorGastos.Entidades
{
    public class HistorialEstado
    {
        public int Id { get; set; }
        public int ComprobanteId { get; set; }
        public EstadoComprobante? EstadoAnterior { get; set; }
        public EstadoComprobante EstadoNuevo { get; set; }
        public string? UsuarioId { get; set; }
        public DateTime Fecha { get; set; }
        public string? Observacion { get; set; }

        public Comprobante Comprobante { get; set; } = null!;
        public Usuario? Usuario { get; set; }
    }
}
