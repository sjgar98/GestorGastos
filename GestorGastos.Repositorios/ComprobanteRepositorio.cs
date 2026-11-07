using GestorGastos.DTOs.Enums;
using Microsoft.EntityFrameworkCore;
using GestorGastos.DTOs;
using GestorGastos.Entidades;
using GestorGastos.Repositorios.Interfaces;
using System.Diagnostics;

namespace GestorGastos.Repositorios
{
    public class ComprobanteRepositorio : IComprobanteRepositorio
    {
        private readonly GestorGastosDbContext _context;

        public ComprobanteRepositorio(GestorGastosDbContext contexto)
        {
            _context = contexto;
        }

        public async Task<(List<Comprobante> Elementos, int Total)> ListarAdminAsync(FiltroAdminComprobantesDto filtro, int tamanoPagina)
        {
            var query = _context.Comprobantes
                .Include(c => c.Usuario)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filtro.Estado))
            {
                if (Enum.TryParse<EstadoComprobante>(filtro.Estado, out var estadoEnum))
                {
                    query = query.Where(c => c.Estado == estadoEnum);
                }
            }

            // Filtro por Empleado específico
            if (!string.IsNullOrEmpty(filtro.EmpleadoId))
            {
                query = query.Where(c => c.UsuarioId == filtro.EmpleadoId);
            }

            // Ordenamiento por Fecha de Carga
            query = filtro.Orden == "antiguos"
                ? query.OrderBy(c => c.FechaCarga)
                : query.OrderByDescending(c => c.FechaCarga);

            var total = await query.CountAsync();

            var saltar = (filtro.Pagina - 1) * tamanoPagina;
            var elementos = await query
                .Skip(saltar)
                .Take(tamanoPagina)
                .ToListAsync();

            return (elementos, total);
        }

        public async Task<Comprobante?> ObtenerConDetalleAsync(int id)
        {
            return await _context.Comprobantes
                .Include(x => x.Usuario)
                .Include(x => x.Historial)
                    .ThenInclude(h => h.Usuario)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<Comprobante?> ObtenerPorIdAsync(int id)
        {
            return await _context.Comprobantes
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

        public async Task<Comprobante?> ObtenerPorIdConUsuarioAsync(int id)
        {
            return await _context.Comprobantes
                .Include(c => c.Usuario)
                .Include(c => c.Historial)
                    .ThenInclude(h => h.Usuario)
                .FirstOrDefaultAsync(c => c.Id == id);
        }
    }
}