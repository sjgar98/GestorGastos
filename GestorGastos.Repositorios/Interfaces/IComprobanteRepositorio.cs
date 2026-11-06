using GestorGastos.Entidades;

namespace GestorGastos.Repositorios.Interfaces
{
    public interface IComprobanteRepositorio
    {
        Task<Comprobante?> ObtenerPorIdAsync(int id);
        Task AgregarAsync(Comprobante comprobante);
        void Eliminar(Comprobante comprobante);
        Task GuardarCambiosAsync();
        Task<Comprobante?> ObtenerPorIdConUsuarioAsync(int id);
    }
}