using GestorGastos.DTOs;
using GestorGastos.DTOs.Enums;
using GestorGastos.Entidades;
using GestorGastos.Repositorios.Interfaces;
using GestorGastos.Servicios.Interfaces;
using Microsoft.Extensions.Logging;
using GestorGastos.DTOs.Helpers;

namespace GestorGastos.Servicios
{
    public class ComprobanteServicio : IComprobanteServicio
    {
        private readonly IComprobanteRepositorio _comprobanteRepositorio;
        private readonly ILogger<ComprobanteServicio> _logger;

        public ComprobanteServicio(
            IComprobanteRepositorio comprobanteRepositorio,
            ILogger<ComprobanteServicio> logger)
        {
            _comprobanteRepositorio = comprobanteRepositorio;
            _logger = logger;
        }

        public async Task<Resultado<ComprobanteAdminDetalleDto>> ObtenerAdminDetalleAsync(int id, string adminId)
        {
            var comprobante = await _comprobanteRepositorio.ObtenerPorIdConUsuarioAsync(id);
            if (comprobante is null)
                return Resultado<ComprobanteAdminDetalleDto>.Fallo(TipoError.NoEncontrado, "El comprobante no existe.");

            return Resultado<ComprobanteAdminDetalleDto>.Exito(AAdminDetalleDto(comprobante, adminId));
        }

        public async Task<Resultado<CambioEstadoRespuestaDto>> TomarEnRevisionAsync(int id, string adminId)
        {
            return await CambiarEstadoAsync(
                id,
                adminId,
                EstadoComprobante.PendienteRevision,
                EstadoComprobante.EnRevision,
                observacion: null,
                fechaPago: null);
        }

        public async Task<Resultado<CambioEstadoRespuestaDto>> AprobarAsync(int id, string adminId)
        {
            return await CambiarEstadoAsync(
                id,
                adminId,
                EstadoComprobante.EnRevision,
                EstadoComprobante.Aprobado,
                observacion: null,
                fechaPago: null);
        }

        public async Task<Resultado<CambioEstadoRespuestaDto>> RechazarAsync(int id, string adminId, string motivo)
        {
            if (string.IsNullOrWhiteSpace(motivo) || motivo.Length < 10 || motivo.Length > 500)
                return Resultado<CambioEstadoRespuestaDto>.Fallo(TipoError.Invalido, "Escribí un motivo de entre 10 y 500 caracteres.");

            return await CambiarEstadoAsync(
                id,
                adminId,
                EstadoComprobante.EnRevision,
                EstadoComprobante.Rechazado,
                observacion: motivo,
                fechaPago: null);
        }

        public async Task<Resultado<CambioEstadoRespuestaDto>> MarcarPagadoAsync(int id, string adminId, DateOnly fechaPago)
        {
            var comprobante = await _comprobanteRepositorio.ObtenerPorIdConUsuarioAsync(id);
            if (comprobante is null)
                return Resultado<CambioEstadoRespuestaDto>.Fallo(TipoError.NoEncontrado, "El comprobante no existe.");

            if (comprobante.UsuarioId == adminId)
                return Resultado<CambioEstadoRespuestaDto>.Fallo(TipoError.Prohibido, "No podés cambiar el estado de tu propio comprobante.");

            if (comprobante.Estado != EstadoComprobante.Aprobado)
                return Resultado<CambioEstadoRespuestaDto>.Fallo(TipoError.Conflicto, "Solo se puede marcar como pagado un comprobante aprobado.");

            var hoy = DateOnly.FromDateTime(DateTime.UtcNow.ConvertirAArgentina());
            var fechaCargaLocal = DateOnly.FromDateTime(comprobante.FechaCarga.ConvertirAArgentina());

            if (fechaPago > hoy)
                return Resultado<CambioEstadoRespuestaDto>.Fallo(TipoError.Invalido, "La fecha de pago no puede ser futura.");

            if (fechaPago < fechaCargaLocal)
                return Resultado<CambioEstadoRespuestaDto>.Fallo(TipoError.Invalido, "La fecha de pago no puede ser anterior a la fecha de carga.");

            comprobante.Estado = EstadoComprobante.Pagado;
            comprobante.FechaPago = fechaPago.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified).ToUniversalTime();

            var historial = new HistorialEstado
            {
                ComprobanteId = comprobante.Id,
                EstadoAnterior = EstadoComprobante.Aprobado,
                EstadoNuevo = EstadoComprobante.Pagado,
                UsuarioId = adminId,
                Fecha = DateTime.UtcNow,
                Observacion = $"Fecha de pago: {fechaPago:dd/MM/yyyy}"
            };
            comprobante.Historial.Add(historial);

            await _comprobanteRepositorio.GuardarCambiosAsync();
            _logger.LogInformation("Comprobante {ComprobanteId} pasó de {Anterior} a {Nuevo}",
                comprobante.Id, EstadoComprobante.Aprobado, EstadoComprobante.Pagado);

            return Resultado<CambioEstadoRespuestaDto>.Exito(ACambioEstadoDto(comprobante));
        }

        // ── Helpers privados ──────────────────────────────────────────────────────

        private async Task<Resultado<CambioEstadoRespuestaDto>> CambiarEstadoAsync(
            int id,
            string adminId,
            EstadoComprobante estadoEsperado,
            EstadoComprobante estadoNuevo,
            string? observacion,
            DateOnly? fechaPago)
        {
            var comprobante = await _comprobanteRepositorio.ObtenerPorIdAsync(id);
            if (comprobante is null)
                return Resultado<CambioEstadoRespuestaDto>.Fallo(TipoError.NoEncontrado, "El comprobante no existe.");

            if (comprobante.UsuarioId == adminId)
                return Resultado<CambioEstadoRespuestaDto>.Fallo(TipoError.Prohibido, "No podés cambiar el estado de tu propio comprobante.");

            if (comprobante.Estado != estadoEsperado)
                return Resultado<CambioEstadoRespuestaDto>.Fallo(TipoError.Conflicto, $"El comprobante no está en estado {estadoEsperado}.");

            var estadoAnterior = comprobante.Estado;
            comprobante.Estado = estadoNuevo;

            if (estadoNuevo == EstadoComprobante.Rechazado)
                comprobante.MotivoRechazo = observacion;

            var historial = new HistorialEstado
            {
                ComprobanteId = comprobante.Id,
                EstadoAnterior = estadoAnterior,
                EstadoNuevo = estadoNuevo,
                UsuarioId = adminId,
                Fecha = DateTime.UtcNow,
                Observacion = observacion
            };
            comprobante.Historial.Add(historial);

            await _comprobanteRepositorio.GuardarCambiosAsync();
            _logger.LogInformation("Comprobante {ComprobanteId} pasó de {Anterior} a {Nuevo}",
                comprobante.Id, estadoAnterior, estadoNuevo);

            return Resultado<CambioEstadoRespuestaDto>.Exito(ACambioEstadoDto(comprobante));
        }

        private static ComprobanteAdminDetalleDto AAdminDetalleDto(Comprobante c, string adminId)
        {
            return new ComprobanteAdminDetalleDto
            {
                Id = c.Id,
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
                Moneda = c.Moneda?.ToString(),
                ConfianzaDescripcion = c.ConfianzaDescripcion,
                ConfianzaMonto = c.ConfianzaMonto,
                EditadoManualmente = c.EditadoManualmente,
                Estado = c.Estado,
                MensajeError = c.MensajeError,
                MotivoRechazo = c.MotivoRechazo,
                FechaCarga = c.FechaCarga,
                FechaPago = c.FechaPago,
                EmpleadoId = c.Usuario?.Id ?? c.UsuarioId,
                EmpleadoNombre = c.Usuario?.Nombre ?? string.Empty,
                EmpleadoEmail = c.Usuario?.Email ?? string.Empty,
                Historial = c.Historial
                    .OrderByDescending(h => h.Fecha)
                    .Select(h => new HistorialEstadoDto
                    {
                        EstadoAnterior = h.EstadoAnterior,
                        EstadoNuevo = h.EstadoNuevo,
                        Fecha = h.Fecha,
                        UsuarioNombre = h.UsuarioId is null ? null : h.Usuario?.Nombre,
                        Observacion = h.Observacion
                    })
                    .ToList()
            };
        }

        private static CambioEstadoRespuestaDto ACambioEstadoDto(Comprobante c)
        {
            return new CambioEstadoRespuestaDto
            {
                Id = c.Id,
                Estado = c.Estado,
                EstadoTexto = c.Estado.Texto()
            };
        }
    }
}