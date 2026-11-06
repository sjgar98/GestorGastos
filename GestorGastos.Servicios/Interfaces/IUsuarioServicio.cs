using GestorGastos.DTOs;
    namespace GestorGastos.Servicios.Interfaces

{
    public interface IUsuarioServicio
    {
        Task RegistrarAccesoAsync(string id, string nombre, string email);
        Task<IEnumerable<UsuarioDto>> ListarEmpleadosAsync();
    }
}
