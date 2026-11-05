using GestorGastos.Repositorios.Interfaces;
using GestorGastos.Servicios.Interfaces;
using GestorGastos.Repositorios;   
using GestorGastos.DTOs;
using GestorGastos.Entidades;

namespace GestorGastos.Servicios
{
    public class ComprobanteServicio : IComprobanteServicio
    {
        private readonly IComprobanteRepositorio _comprobanteRepositorio;

        public ComprobanteServicio(IComprobanteRepositorio comprobanteRepositorio)
        {
            _comprobanteRepositorio = comprobanteRepositorio;
        }

        public async Task<Resultado<ComprobanteDetalleDto>> ObtenerDetalleAsync(int comprobanteId, string usuarioId)
        {
            var comprobante = await _comprobanteRepositorio.ObtenerPorIdAsync(comprobanteId);

            if (comprobante == null || comprobante.UsuarioId != usuarioId)
            {
                return Resultado<ComprobanteDetalleDto>.Fallo(TipoError.NoEncontrado, "El comprobante no existe.");
            }

            return Resultado<ComprobanteDetalleDto>.Exito(ADto(comprobante));
        }

        private static ComprobanteDetalleDto ADto(Comprobante comprobante)
        {
            return new ComprobanteDetalleDto
            {
                Id = comprobante.Id,
                NombreArchivoOriginal = comprobante.NombreArchivoOriginal,
                ContentType = comprobante.ContentType,
                TipoDocumento = comprobante.TipoDocumento,
                Categoria = comprobante.Categoria,
                Estado = comprobante.Estado,
                Comercio = comprobante.Comercio,
                FechaComprobante = comprobante.FechaComprobante,
                Descripcion = comprobante.Descripcion,
                Monto = comprobante.Monto,
                Moneda = comprobante.Moneda,
                MensajeError = comprobante.MensajeError,
                MotivoRechazo = comprobante.MotivoRechazo,
                FechaCarga = comprobante.FechaCarga,
                FechaPago = comprobante.FechaPago,
                EditadoManualmente = comprobante.EditadoManualmente,
                Historial = comprobante.Historial
                    .OrderByDescending(h => h.Fecha)
                    .Select(h => new HistorialEstadoDto
                    {
                        EstadoAnterior = h.EstadoAnterior,
                        EstadoNuevo = h.EstadoNuevo,
                        Fecha = h.Fecha,
                        NombreUsuario = h.Usuario?.Nombre,
                        Observacion = h.Observacion
                    })
                    .ToList()
            };
        }


    }
}