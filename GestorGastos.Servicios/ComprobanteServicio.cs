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
        private static readonly TimeSpan TiempoMaximoProcesando = TimeSpan.FromMinutes(5);
        private const string MensajeTiempoAgotado = "El análisis tardó demasiado. Completá los datos manualmente.";
        private const string MensajeNoEncontrado = "El comprobante no existe.";
        private const string MensajeYaProcesado = "El comprobante ya no está en Procesando.";
        private const string MensajeSinMonto = "No se detectó el monto total del comprobante.";
        private const string MensajeSinDescripcion = "No se detectó la descripción del comprobante.";
        private const decimal MontoMaximo = 99_999_999.99m;
        private const int LargoMaximoComercio = 200;
        private const int LargoMaximoTexto = 500;

        private readonly IComprobanteRepositorio _comprobanteRepositorio;
        private readonly IAlmacenamientoArchivos _almacenamientoArchivos;
        private readonly ILogger<ComprobanteServicio> _logger;

        public ComprobanteServicio(
            IComprobanteRepositorio comprobanteRepositorio,
            IAlmacenamientoArchivos almacenamientoArchivos,
            ILogger<ComprobanteServicio> logger)
        {
            _comprobanteRepositorio = comprobanteRepositorio;
            _almacenamientoArchivos = almacenamientoArchivos;
            _logger = logger;
        }

        public async Task<Resultado<CargaRealizadaDto>> CargarAsync(string usuarioId, CargaComprobanteDto datos, Stream contenido)
        {
            using var memoria = new MemoryStream();
            await contenido.CopyToAsync(memoria);
            var bytes = memoria.ToArray();

            var validacion = ValidadorArchivo.Validar(datos.NombreArchivo, bytes);
            if (!validacion.Exitoso)
                return Resultado<CargaRealizadaDto>.Fallo(validacion.Error!.Value, validacion.Mensaje!);

            var archivo = validacion.Valor!;
            var ahora = DateTime.UtcNow;
            var comprobante = new Comprobante
            {
                UsuarioId = usuarioId,
                NombreArchivoOriginal = AcortarNombre(Path.GetFileName(datos.NombreArchivo)),
                BlobNombre = $"{Guid.NewGuid()}{archivo.Extension}",
                ContentType = archivo.ContentType,
                TamanioBytes = bytes.Length,
                TipoDocumento = datos.TipoDocumento,
                Categoria = datos.Categoria,
                Estado = EstadoComprobante.Procesando,
                FechaCarga = ahora
            };
            comprobante.Historial.Add(new HistorialEstado
            {
                EstadoAnterior = null,
                EstadoNuevo = EstadoComprobante.Procesando,
                UsuarioId = usuarioId,
                Fecha = ahora
            });

            await _comprobanteRepositorio.AgregarAsync(comprobante);
            await _comprobanteRepositorio.GuardarCambiosAsync();

            try
            {
                using var contenidoValidado = new MemoryStream(bytes);
                await _almacenamientoArchivos.SubirAsync(comprobante.BlobNombre, contenidoValidado, archivo.ContentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo subir el archivo del comprobante {ComprobanteId} ({BlobNombre}); se borra el registro",
                    comprobante.Id, comprobante.BlobNombre);
                _comprobanteRepositorio.Eliminar(comprobante);
                await _comprobanteRepositorio.GuardarCambiosAsync();
                return Resultado<CargaRealizadaDto>.Fallo(TipoError.Invalido, "No pudimos guardar el archivo. Intentá de nuevo en unos minutos.");
            }

            _logger.LogInformation("Comprobante {ComprobanteId} cargado por {UsuarioId}: {NombreArchivo} ({TamanioBytes} bytes, {TipoDocumento}) en {BlobNombre}",
                comprobante.Id, usuarioId, comprobante.NombreArchivoOriginal, comprobante.TamanioBytes, comprobante.TipoDocumento, comprobante.BlobNombre);

            return Resultado<CargaRealizadaDto>.Exito(new CargaRealizadaDto
            {
                Id = comprobante.Id,
                SoloSeAnalizanDosPaginas = archivo.CantidadPaginas > ValidadorArchivo.PaginasAnalizadas
            });
        }

        public async Task<Resultado<EstadoComprobanteDto>> ObtenerEstadoAsync(int id, string usuarioId)
        {
            var comprobante = await _comprobanteRepositorio.ObtenerPorIdAsync(id);
            if (comprobante is null || comprobante.UsuarioId != usuarioId)
                return Resultado<EstadoComprobanteDto>.Fallo(TipoError.NoEncontrado, MensajeNoEncontrado);

            if (comprobante.Estado == EstadoComprobante.Procesando
                && DateTime.UtcNow - comprobante.FechaCarga > TiempoMaximoProcesando)
            {
                comprobante.MensajeError = MensajeTiempoAgotado;
                RegistrarCambioEstado(comprobante, EstadoComprobante.ErrorExtraccion, usuarioId: null, MensajeTiempoAgotado);
                await _comprobanteRepositorio.GuardarCambiosAsync();
            }

            return Resultado<EstadoComprobanteDto>.Exito(AEstadoDto(comprobante));
        }

        public async Task<Resultado<EstadoComprobanteDto>> EditarAsync(int id, string usuarioId, EditarComprobanteDto datos)
        {
            var comprobante = await _comprobanteRepositorio.ObtenerPorIdAsync(id);
            if (comprobante is null || comprobante.UsuarioId != usuarioId)
                return Resultado<EstadoComprobanteDto>.Fallo(TipoError.NoEncontrado, MensajeNoEncontrado);

            if (!EsEditablePorElDuenio(comprobante.Estado))
                return Resultado<EstadoComprobanteDto>.Fallo(TipoError.Conflicto,
                    "El comprobante ya no se puede editar: solo se editan los que están Pendientes de Revisión o con Error de extracción.");

            var comercio = string.IsNullOrWhiteSpace(datos.Comercio) ? null : datos.Comercio.Trim();
            var descripcion = datos.Descripcion!.Trim();

            var cambioAlgunDatoExtraido = comprobante.Comercio != comercio
                || comprobante.FechaComprobante != datos.FechaComprobante
                || comprobante.Descripcion != descripcion
                || comprobante.Monto != datos.Monto
                || comprobante.Moneda != datos.Moneda;

            comprobante.Comercio = comercio;
            comprobante.FechaComprobante = datos.FechaComprobante;
            comprobante.Descripcion = descripcion;
            comprobante.Monto = datos.Monto;
            comprobante.Moneda = datos.Moneda;
            comprobante.Categoria = datos.Categoria!.Value;

            if (cambioAlgunDatoExtraido)
                comprobante.EditadoManualmente = true;

            if (comprobante.Estado == EstadoComprobante.ErrorExtraccion)
            {
                comprobante.MensajeError = null;
                RegistrarCambioEstado(comprobante, EstadoComprobante.PendienteRevision, usuarioId, "Datos completados manualmente.");
            }

            await _comprobanteRepositorio.GuardarCambiosAsync();
            return Resultado<EstadoComprobanteDto>.Exito(AEstadoDto(comprobante));
        }

        public async Task<Resultado<bool>> EliminarAsync(int id, string usuarioId)
        {
            var comprobante = await _comprobanteRepositorio.ObtenerPorIdAsync(id);
            if (comprobante is null || comprobante.UsuarioId != usuarioId)
                return Resultado<bool>.Fallo(TipoError.NoEncontrado, MensajeNoEncontrado);

            if (!EsEditablePorElDuenio(comprobante.Estado))
                return Resultado<bool>.Fallo(TipoError.Conflicto,
                    "El comprobante ya no se puede eliminar: solo se eliminan los que están Pendientes de Revisión o con Error de extracción.");

            await _almacenamientoArchivos.EliminarAsync(comprobante.BlobNombre);
            _comprobanteRepositorio.Eliminar(comprobante);
            await _comprobanteRepositorio.GuardarCambiosAsync();

            _logger.LogInformation("Comprobante {ComprobanteId} eliminado por {UsuarioId} (blob {BlobNombre})",
                comprobante.Id, usuarioId, comprobante.BlobNombre);
            return Resultado<bool>.Exito(true);
        }

        public async Task<Resultado<ComprobanteParaExtraccion>> ObtenerParaExtraccionAsync(string blobNombre)
        {
            var comprobante = await _comprobanteRepositorio.ObtenerPorBlobNombreSinSeguimientoAsync(blobNombre);
            if (comprobante is null)
                return Resultado<ComprobanteParaExtraccion>.Fallo(TipoError.NoEncontrado, MensajeNoEncontrado);

            if (comprobante.Estado != EstadoComprobante.Procesando)
                return Resultado<ComprobanteParaExtraccion>.Fallo(TipoError.Conflicto, MensajeYaProcesado);

            return Resultado<ComprobanteParaExtraccion>.Exito(new ComprobanteParaExtraccion
            {
                Id = comprobante.Id,
                NombreArchivoOriginal = comprobante.NombreArchivoOriginal,
                TipoDocumento = comprobante.TipoDocumento
            });
        }

        public async Task<Resultado<bool>> RegistrarExtraccionAsync(string blobNombre, DatosExtraidos datos)
        {
            var comprobante = await _comprobanteRepositorio.ObtenerPorBlobNombreAsync(blobNombre);
            if (comprobante is null)
                return Resultado<bool>.Fallo(TipoError.NoEncontrado, MensajeNoEncontrado);

            if (comprobante.Estado != EstadoComprobante.Procesando)
                return Resultado<bool>.Fallo(TipoError.Conflicto, MensajeYaProcesado);

            comprobante.Comercio = NormalizarTexto(datos.Comercio, LargoMaximoComercio);
            comprobante.FechaComprobante = DescartarFechaFutura(datos.FechaComprobante);
            comprobante.Descripcion = NormalizarTexto(datos.Descripcion, LargoMaximoTexto);
            comprobante.Monto = NormalizarMonto(datos.Monto);
            comprobante.Moneda = datos.Moneda ?? Moneda.ARS;
            comprobante.ConfianzaDescripcion = datos.ConfianzaDescripcion;
            comprobante.ConfianzaMonto = datos.ConfianzaMonto;

            var mensajeError = comprobante.Monto is null ? MensajeSinMonto
                : comprobante.Descripcion is null ? MensajeSinDescripcion
                : null;

            if (mensajeError is null)
            {
                RegistrarCambioEstado(comprobante, EstadoComprobante.PendienteRevision, usuarioId: null, observacion: null);
            }
            else
            {
                comprobante.MensajeError = mensajeError;
                RegistrarCambioEstado(comprobante, EstadoComprobante.ErrorExtraccion, usuarioId: null, mensajeError);
            }

            await _comprobanteRepositorio.GuardarCambiosAsync();
            return Resultado<bool>.Exito(true);
        }

        public async Task<Resultado<bool>> RegistrarFallaExtraccionAsync(string blobNombre, string mensajeError)
        {
            var comprobante = await _comprobanteRepositorio.ObtenerPorBlobNombreAsync(blobNombre);
            if (comprobante is null)
                return Resultado<bool>.Fallo(TipoError.NoEncontrado, MensajeNoEncontrado);

            if (comprobante.Estado != EstadoComprobante.Procesando)
                return Resultado<bool>.Fallo(TipoError.Conflicto, MensajeYaProcesado);

            comprobante.MensajeError = mensajeError;
            RegistrarCambioEstado(comprobante, EstadoComprobante.ErrorExtraccion, usuarioId: null, mensajeError);

            await _comprobanteRepositorio.GuardarCambiosAsync();
            return Resultado<bool>.Exito(true);
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

        private static bool EsEditablePorElDuenio(EstadoComprobante estado)
        {
            return estado is EstadoComprobante.PendienteRevision or EstadoComprobante.ErrorExtraccion;
        }

        private static string? NormalizarTexto(string? texto, int largoMaximo)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return null;

            var recortado = texto.Trim();
            return recortado.Length <= largoMaximo ? recortado : recortado[..largoMaximo];
        }

        private static DateOnly? DescartarFechaFutura(DateOnly? fecha)
        {
            var hoy = DateOnly.FromDateTime(DateTime.UtcNow.ConvertirAArgentina());
            return fecha > hoy ? null : fecha;
        }

        private static decimal? NormalizarMonto(decimal? monto)
        {
            if (monto is null)
                return null;

            var redondeado = Math.Round(monto.Value, 2);
            return redondeado > 0 && redondeado <= MontoMaximo ? redondeado : null;
        }

        private static string AcortarNombre(string nombreArchivo)
        {
            const int largoMaximo = 255;
            if (nombreArchivo.Length <= largoMaximo)
                return nombreArchivo;

            var extension = Path.GetExtension(nombreArchivo);
            return nombreArchivo[..(largoMaximo - extension.Length)] + extension;
        }

        private void RegistrarCambioEstado(Comprobante comprobante, EstadoComprobante estadoNuevo, string? usuarioId, string? observacion)
        {
            var estadoAnterior = comprobante.Estado;
            comprobante.Estado = estadoNuevo;
            comprobante.Historial.Add(new HistorialEstado
            {
                ComprobanteId = comprobante.Id,
                EstadoAnterior = estadoAnterior,
                EstadoNuevo = estadoNuevo,
                UsuarioId = usuarioId,
                Fecha = DateTime.UtcNow,
                Observacion = observacion
            });

            _logger.LogInformation("Comprobante {ComprobanteId} pasó de {Anterior} a {Nuevo}",
                comprobante.Id, estadoAnterior, estadoNuevo);
        }

        private static EstadoComprobanteDto AEstadoDto(Comprobante c)
        {
            return new EstadoComprobanteDto
            {
                Id = c.Id,
                NombreArchivoOriginal = c.NombreArchivoOriginal,
                Estado = c.Estado,
                EstadoTexto = c.Estado.Texto(),
                TipoDocumento = c.TipoDocumento,
                Categoria = c.Categoria,
                Comercio = c.Comercio,
                FechaComprobante = c.FechaComprobante,
                Descripcion = c.Descripcion,
                Monto = c.Monto,
                Moneda = c.Moneda,
                ConfianzaDescripcion = c.ConfianzaDescripcion,
                ConfianzaMonto = c.ConfianzaMonto,
                MensajeError = c.MensajeError,
                Editable = EsEditablePorElDuenio(c.Estado)
            };
        }

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