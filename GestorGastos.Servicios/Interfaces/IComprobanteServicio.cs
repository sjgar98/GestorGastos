using GestorGastos.DTOs;

namespace GestorGastos.Servicios.Interfaces
{
    public interface IComprobanteServicio
    {
        Task<PaginadoDto<ComprobanteAdminListadoDto>> ListarAdminAsync(FiltroAdminComprobantesDto filtro);
        Task<PaginadoDto<ComprobanteListadoDto>> ListarMisComprobantesAsync(string usuarioId, FiltroMisComprobantesDto filtro);
        Task<ResumenDashboardDto> ObtenerResumenAsync(string usuarioId);

        Task<Resultado<CargaRealizadaDto>> CargarAsync(string usuarioId, CargaComprobanteDto datos, Stream contenido);
        Task<Resultado<EstadoComprobanteDto>> ObtenerEstadoAsync(int id, string usuarioId);
        Task<Resultado<EstadoComprobanteDto>> EditarAsync(int id, string usuarioId, EditarComprobanteDto datos);
        Task<Resultado<bool>> EliminarAsync(int id, string usuarioId);

        Task<Resultado<ComprobanteDetalleDto>> ObtenerDetalleAsync(int comprobanteId, string usuarioId);

        Task<Resultado<ComprobanteParaExtraccion>> ObtenerParaExtraccionAsync(string blobNombre);
        Task<Resultado<bool>> RegistrarExtraccionAsync(string blobNombre, DatosExtraidos datos);
        Task<Resultado<bool>> RegistrarFallaExtraccionAsync(string blobNombre, string mensajeError);

        Task<Resultado<ComprobanteAdminDetalleDto>> ObtenerAdminDetalleAsync(int id);
        Task<Resultado<CambioEstadoRespuestaDto>> TomarEnRevisionAsync(int id, string adminId);
        Task<Resultado<CambioEstadoRespuestaDto>> AprobarAsync(int id, string adminId);
        Task<Resultado<CambioEstadoRespuestaDto>> RechazarAsync(int id, string adminId, string motivo);
        Task<Resultado<CambioEstadoRespuestaDto>> MarcarPagadoAsync(int id, string adminId, DateOnly fechaPago);
        Task<Resultado<UrlArchivoDto>> ObtenerUrlArchivoAsync(int comprobanteId, string usuarioId, bool esAdministrador, bool descarga);
    }
}