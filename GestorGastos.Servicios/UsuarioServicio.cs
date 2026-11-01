using GestorGastos.Entidades;
using GestorGastos.Repositorios.Interfaces;
using GestorGastos.Servicios.Interfaces;

namespace GestorGastos.Servicios
{
    public class UsuarioServicio : IUsuarioServicio
    {
        private readonly IUsuarioRepositorio _usuarioRepositorio;

        public UsuarioServicio(IUsuarioRepositorio usuarioRepositorio)
        {
            _usuarioRepositorio = usuarioRepositorio;
        }

        public async Task RegistrarAccesoAsync(string id, string nombre, string email)
        {
            var usuario = await _usuarioRepositorio.ObtenerPorIdAsync(id);
            if (usuario == null)
            {
                usuario = new Usuario { Id = id };
                await _usuarioRepositorio.AgregarAsync(usuario);
            }

            usuario.Nombre = nombre;
            usuario.Email = email;
            usuario.UltimoAcceso = DateTime.UtcNow;
            await _usuarioRepositorio.GuardarCambiosAsync();
        }
    }
}
