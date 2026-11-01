using GestorGastos.Entidades;
using GestorGastos.Repositorios.Interfaces;

namespace GestorGastos.Repositorios
{
    public class UsuarioRepositorio : IUsuarioRepositorio
    {
        private readonly GestorGastosDbContext _contexto;

        public UsuarioRepositorio(GestorGastosDbContext contexto)
        {
            _contexto = contexto;
        }

        public async Task<Usuario?> ObtenerPorIdAsync(string id)
        {
            return await _contexto.Usuarios.FindAsync(id);
        }

        public async Task AgregarAsync(Usuario usuario)
        {
            await _contexto.Usuarios.AddAsync(usuario);
        }

        public async Task GuardarCambiosAsync()
        {
            await _contexto.SaveChangesAsync();
        }
    }
}
