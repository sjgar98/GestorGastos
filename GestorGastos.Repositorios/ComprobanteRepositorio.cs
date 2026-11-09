using GestorGastos.DTOs;
using GestorGastos.DTOs.Enums;
using GestorGastos.Entidades;
using GestorGastos.Repositorios.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GestorGastos.Repositorios
{
    public class ComprobanteRepositorio : IComprobanteRepositorio
    {
        private const string OrdenAntiguos = "antiguos";

        private readonly GestorGastosDbContext _context;

        public ComprobanteRepositorio(GestorGastosDbContext contexto)
        {
            _context = contexto;
        }

        public async Task<(List<Comprobante> Elementos, int Total)> ListarAdminAsync(FiltroAdminComprobantesDto filtro, int tamanoPagina)
        {
            var query = _context.Comprobantes
                .AsNoTracking()
                .Include(c => c.Usuario)
                .AsQueryable();

            if (!string.IsNullOrEmpty(filtro.EmpleadoId))
                query = query.Where(c => c.UsuarioId == filtro.EmpleadoId);

            return await FiltrarOrdenarYPaginarAsync(query, filtro.Estado, filtro.Orden, filtro.Pagina, tamanoPagina);
        }

        public async Task<(List<Comprobante> Elementos, int Total)> ListarPorUsuarioAsync(string usuarioId, FiltroMisComprobantesDto filtro, int tamanoPagina)
        {
            var query = _context.Comprobantes
                .AsNoTracking()
                .Where(c => c.UsuarioId == usuarioId);

            return await FiltrarOrdenarYPaginarAsync(query, filtro.Estado, filtro.Orden, filtro.Pagina, tamanoPagina);
        }

        public async Task<Dictionary<EstadoComprobante, int>> ContarPorEstadoAsync(string usuarioId)
        {
            return await _context.Comprobantes
                .AsNoTracking()
                .Where(c => c.UsuarioId == usuarioId)
                .GroupBy(c => c.Estado)
                .Select(g => new { Estado = g.Key, Cantidad = g.Count() })
                .ToDictionaryAsync(x => x.Estado, x => x.Cantidad);
        }

        public async Task<Comprobante?> ObtenerPorIdAsync(int id)
        {
            return await _context.Comprobantes
                .Include(c => c.Usuario)
                .Include(c => c.Historial)
                    .ThenInclude(h => h.Usuario)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Comprobante?> ObtenerPorBlobNombreAsync(string blobNombre)
        {
            return await _context.Comprobantes
                .Include(c => c.Historial)
                .FirstOrDefaultAsync(c => c.BlobNombre == blobNombre);
        }

        public async Task<Comprobante?> ObtenerPorBlobNombreSinSeguimientoAsync(string blobNombre)
        {
            return await _context.Comprobantes
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.BlobNombre == blobNombre);
        }

        public async Task AgregarAsync(Comprobante comprobante)
        {
            await _context.Comprobantes.AddAsync(comprobante);
        }

        public void Eliminar(Comprobante comprobante)
        {
            _context.Comprobantes.Remove(comprobante);
        }

        public async Task GuardarCambiosAsync()
        {
            await _context.SaveChangesAsync();
        }

        private static async Task<(List<Comprobante> Elementos, int Total)> FiltrarOrdenarYPaginarAsync(
            IQueryable<Comprobante> query, EstadoComprobante? estado, string? orden, int pagina, int tamanoPagina)
        {
            if (estado is not null)
                query = query.Where(c => c.Estado == estado);

            // El desempate por Id mantiene el orden estable entre páginas cuando dos comprobantes
            // comparten la misma fecha de carga.
            query = orden == OrdenAntiguos
                ? query.OrderBy(c => c.FechaCarga).ThenBy(c => c.Id)
                : query.OrderByDescending(c => c.FechaCarga).ThenByDescending(c => c.Id);

            var total = await query.CountAsync();
            var elementos = await query
                .Skip((pagina - 1) * tamanoPagina)
                .Take(tamanoPagina)
                .ToListAsync();

            return (elementos, total);
        }
    }
}
