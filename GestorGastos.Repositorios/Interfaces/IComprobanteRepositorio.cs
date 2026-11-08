using GestorGastos.DTOs;
using GestorGastos.DTOs.Enums;
using GestorGastos.Entidades;

namespace GestorGastos.Repositorios.Interfaces
{
    public interface IComprobanteRepositorio
    {
        Task<(List<Comprobante> Elementos, int Total)> ListarAdminAsync(FiltroAdminComprobantesDto filtro, int tamanoPagina);
        Task<(List<Comprobante> Elementos, int Total)> ListarPorUsuarioAsync(string usuarioId, FiltroMisComprobantesDto filtro, int tamanoPagina);
        Task<Dictionary<EstadoComprobante, int>> ContarPorEstadoAsync(string usuarioId);
        Task<Comprobante?> ObtenerPorIdAsync(int id);
        Task<Comprobante?> ObtenerPorBlobNombreAsync(string blobNombre);
        Task<Comprobante?> ObtenerPorBlobNombreSinSeguimientoAsync(string blobNombre);
        Task AgregarAsync(Comprobante comprobante);
        void Eliminar(Comprobante comprobante);
        Task GuardarCambiosAsync();
        Task<Comprobante?> ObtenerPorIdConUsuarioAsync(int id);
    }
}