using GestorGastos.DTOs;

namespace GestorGastos.Servicios.Interfaces
{
    public interface IComprobanteServicio
    {
        Task<Resultado<CargaRealizadaDto>> CargarAsync(string usuarioId, CargaComprobanteDto datos, Stream contenido);
        Task<Resultado<EstadoComprobanteDto>> ObtenerEstadoAsync(int id, string usuarioId);
        Task<Resultado<EstadoComprobanteDto>> EditarAsync(int id, string usuarioId, EditarComprobanteDto datos);
        Task<Resultado<bool>> EliminarAsync(int id, string usuarioId);

        Task<Resultado<ComprobanteDetalleDto>> ObtenerDetalleAsync(int comprobanteId, string usuarioId);
        Task<Resultado<ComprobanteAdminDetalleDto>> ObtenerAdminDetalleAsync(int id, string adminId);
        Task<Resultado<CambioEstadoRespuestaDto>> TomarEnRevisionAsync(int id, string adminId);
        Task<Resultado<CambioEstadoRespuestaDto>> AprobarAsync(int id, string adminId);
        Task<Resultado<CambioEstadoRespuestaDto>> RechazarAsync(int id, string adminId, string motivo);
        Task<Resultado<CambioEstadoRespuestaDto>> MarcarPagadoAsync(int id, string adminId, DateOnly fechaPago);
    }
}