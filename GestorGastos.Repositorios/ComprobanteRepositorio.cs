using GestorGastos.Entidades;
using GestorGastos.Repositorios.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GestorGastos.Repositorios
{
    public class ComprobanteRepositorio : IComprobanteRepositorio
    {
        private readonly GestorGastosDbContext _contexto;

        public ComprobanteRepositorio(GestorGastosDbContext contexto)
        {
            _contexto = contexto;
        }

        public async Task<Comprobante?> ObtenerPorIdAsync(int id)
        {
            return await _contexto.Comprobantes
                .Include(c => c.Historial)
                .ThenInclude(h => h.Usuario)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task AgregarAsync(Comprobante comprobante)
        {
            await _contexto.Comprobantes.AddAsync(comprobante);
        }

        public async Task GuardarCambiosAsync()
        {
            await _contexto.SaveChangesAsync();
        }

        public async Task<Comprobante?> ObtenerPorIdConUsuarioAsync(int id)
        {
            return await _contexto.Comprobantes
                .Include(c => c.Usuario)
                .Include(c => c.Historial)
                    .ThenInclude(h => h.Usuario)
                .FirstOrDefaultAsync(c => c.Id == id);
        }
    }
}