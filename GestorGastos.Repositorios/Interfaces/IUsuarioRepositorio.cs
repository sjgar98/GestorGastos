using GestorGastos.Entidades;

namespace GestorGastos.Repositorios.Interfaces
{
    public interface IUsuarioRepositorio
    {
        Task<Usuario?> ObtenerPorIdAsync(string id);
        Task AgregarAsync(Usuario usuario);
        Task GuardarCambiosAsync();
        Task<List<Usuario>> ListarTodosAsync();
    }
}
