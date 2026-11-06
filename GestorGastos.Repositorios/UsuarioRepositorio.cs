using GestorGastos.Entidades;
using GestorGastos.Repositorios.Interfaces;
using Microsoft.EntityFrameworkCore;

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

        public async Task<List<Usuario>> ListarTodosAsync()
        {
            return await _contexto.Usuarios
                .AsNoTracking()
                .OrderBy(u => u.Nombre)
                .ToListAsync();
        }
    }
}
