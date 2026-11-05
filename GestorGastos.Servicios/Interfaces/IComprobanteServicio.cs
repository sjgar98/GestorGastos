using GestorGastos.DTOs;

namespace GestorGastos.Servicios.Interfaces
{
    public interface IComprobanteServicio
    {
        Task<Resultado<ComprobanteDetalleDto>> ObtenerDetalleAsync(int comprobanteId, string usuarioId);
    }
}