using GestorGastos.DTOs;

namespace GestorGastos.Servicios.Interfaces
{
    public interface IComprobanteServicio
    {
        // TODO Almacenamiento: CargarAsync depende de IAlmacenamientoArchivos (pendiente)
        // TODO Extracción: los métodos de P4 (CargarAsync, ObtenerEstadoAsync) van acá

        Task<Resultado<ComprobanteAdminDetalleDto>> ObtenerAdminDetalleAsync(int id, string adminId);
        Task<Resultado<CambioEstadoRespuestaDto>> TomarEnRevisionAsync(int id, string adminId);
        Task<Resultado<CambioEstadoRespuestaDto>> AprobarAsync(int id, string adminId);
        Task<Resultado<CambioEstadoRespuestaDto>> RechazarAsync(int id, string adminId, string motivo);
        Task<Resultado<CambioEstadoRespuestaDto>> MarcarPagadoAsync(int id, string adminId, DateOnly fechaPago);
    }
}
