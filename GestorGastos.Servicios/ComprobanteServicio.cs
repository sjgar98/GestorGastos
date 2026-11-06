using GestorGastos.DTOs;
using GestorGastos.DTOs.Admin;
using GestorGastos.Repositorios.Interfaces;
using GestorGastos.Servicios.Interfaces;

namespace GestorGastos.Servicios
{
    public class ComprobanteServicio : IComprobanteServicio
    {
        private readonly IComprobanteRepositorio _comprobanteRepositorio;
        private const int TamanoPaginaDefault = 10;

        public ComprobanteServicio(IComprobanteRepositorio comprobanteRepositorio)
        {
            _comprobanteRepositorio = comprobanteRepositorio;
        }

        public async Task<PaginadoDto<ComprobanteAdminListadoDto>> ListarAdminAsync(FiltroAdminComprobantesDto filtro)
        {
            if (filtro.Pagina < 1) filtro.Pagina = 1;

            var (elementosEntidad, total) = await _comprobanteRepositorio.ListarAdminAsync(filtro, TamanoPaginaDefault);

            var elementosDto = elementosEntidad.Select(c => new ComprobanteAdminListadoDto
            {
                Id = c.Id,
                UsuarioId = c.UsuarioId,
                NombreEmpleado = c.Usuario?.Nombre ?? "Desconocido",
                EmailEmpleado = c.Usuario?.Email ?? string.Empty,
                NombreArchivoOriginal = c.NombreArchivoOriginal,
                FechaCarga = c.FechaCarga,
                Monto = c.Monto,
                Moneda = c.Moneda.ToString(),
                Estado = c.Estado.ToString()
            }).ToList();

            return new PaginadoDto<ComprobanteAdminListadoDto>
            {
                Elementos = elementosDto,
                TotalRegistros = total,
                PaginaActual = filtro.Pagina,
                TamanoPagina = TamanoPaginaDefault
            };
        }

        public async Task<ComprobanteAdminDetalleDto?> ObtenerDetalleAdminAsync(int id)
        {
            // Usamos el repositorio en lugar de _context directamente
            var c = await _comprobanteRepositorio.ObtenerConDetalleAsync(id);

            if (c == null) return null;

            return new ComprobanteAdminDetalleDto
            {
                Id = c.Id,
                UsuarioId = c.UsuarioId,
                NombreEmpleado = c.Usuario?.Nombre ?? "Desconocido",
                EmailEmpleado = c.Usuario?.Email ?? "",
                NombreArchivoOriginal = c.NombreArchivoOriginal,
                BlobNombre = c.BlobNombre,
                ContentType = c.ContentType,
                TamanioBytes = c.TamanioBytes,
                TipoDocumento = c.TipoDocumento,
                Categoria = c.Categoria,
                Comercio = c.Comercio,
                FechaComprobante = c.FechaComprobante,
                Descripcion = c.Descripcion,
                Monto = c.Monto,
                Moneda = c.Moneda,
                ConfianzaDescripcion = c.ConfianzaDescripcion,
                ConfianzaMonto = c.ConfianzaMonto,
                EditadoManualmente = c.EditadoManualmente,
                Estado = c.Estado,
                MensajeError = c.MensajeError,
                MotivoRechazo = c.MotivoRechazo,
                FechaCarga = c.FechaCarga,
                FechaPago = c.FechaPago,
                Historial = c.Historial.OrderByDescending(h => h.Fecha).Select(h => new HistorialItemDto
                {
                    EstadoAnterior = h.EstadoAnterior,
                    EstadoNuevo = h.EstadoNuevo,
                    NombreUsuario = h.Usuario?.Nombre ?? "Sistema",
                    Fecha = h.Fecha,
                    Observacion = h.Observacion
                }).ToList()
            };
        }
    }
}