using System.Diagnostics;
using GestorGastos.Servicios.Interfaces;
using Microsoft.Extensions.Logging;

namespace GestorGastos.Servicios
{
    public class ExtraccionServicio : IExtraccionServicio
    {
        private const string MensajeFallaServicio = "El servicio de extracción no pudo analizar el comprobante.";

        private readonly IComprobanteServicio _comprobanteServicio;
        private readonly IExtractorDocumentos _extractorDocumentos;
        private readonly ILogger<ExtraccionServicio> _logger;

        public ExtraccionServicio(
            IComprobanteServicio comprobanteServicio,
            IExtractorDocumentos extractorDocumentos,
            ILogger<ExtraccionServicio> logger)
        {
            _comprobanteServicio = comprobanteServicio;
            _extractorDocumentos = extractorDocumentos;
            _logger = logger;
        }

        public async Task ProcesarAsync(string blobNombre, Stream contenido, CancellationToken cancelacion)
        {
            var pendiente = await _comprobanteServicio.ObtenerParaExtraccionAsync(blobNombre);
            if (!pendiente.Exitoso)
            {
                _logger.LogWarning("Se omite la extracción de {BlobNombre}: {Motivo}", blobNombre, pendiente.Mensaje);
                return;
            }

            var comprobante = pendiente.Valor!;
            _logger.LogInformation("Inicio de extracción del comprobante {ComprobanteId} ({TipoDocumento})",
                comprobante.Id, comprobante.TipoDocumento);
            var cronometro = Stopwatch.StartNew();

            DatosExtraidos? datos = null;
            try
            {
                datos = await _extractorDocumentos.ExtraerAsync(
                    contenido, comprobante.TipoDocumento, comprobante.NombreArchivoOriginal, cancelacion);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error de extracción del comprobante {ComprobanteId} tras {DuracionMs} ms",
                    comprobante.Id, cronometro.ElapsedMilliseconds);
            }

            var registro = datos is null
                ? await _comprobanteServicio.RegistrarFallaExtraccionAsync(blobNombre, MensajeFallaServicio)
                : await _comprobanteServicio.RegistrarExtraccionAsync(blobNombre, datos);

            if (!registro.Exitoso)
                _logger.LogWarning("No se guardó la extracción de {BlobNombre}: {Motivo}", blobNombre, registro.Mensaje);

            _logger.LogInformation("Fin de extracción del comprobante {ComprobanteId} en {DuracionMs} ms",
                comprobante.Id, cronometro.ElapsedMilliseconds);
        }
    }
}
