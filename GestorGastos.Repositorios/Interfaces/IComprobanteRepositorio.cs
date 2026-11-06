using GestorGastos.DTOs;
using GestorGastos.Entidades;

namespace GestorGastos.Repositorios.Interfaces
{
    public interface IComprobanteRepositorio
    {
        // ... otros métodos existentes ...
        Task<(List<Comprobante> Elementos, int Total)> ListarAdminAsync(FiltroAdminComprobantesDto filtro, int tamanoPagina);
        Task<Comprobante?> ObtenerConDetalleAsync(int id);
    }
}