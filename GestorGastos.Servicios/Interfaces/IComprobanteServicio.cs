using GestorGastos.DTOs;
using GestorGastos.DTOs.Admin;

namespace GestorGastos.Servicios.Interfaces
{
    public interface IComprobanteServicio
    {
        // ... otros métodos existentes ...
        Task<PaginadoDto<ComprobanteAdminListadoDto>> ListarAdminAsync(FiltroAdminComprobantesDto filtro);
        Task<ComprobanteAdminDetalleDto?> ObtenerDetalleAdminAsync(int id);
    }
}