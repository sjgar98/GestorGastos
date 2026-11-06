using GestorGastos.DTOs.Enums;
using GestorGastos.DTOs.Helpers;
using GestorGastos.Servicios.Interfaces;
using Microsoft.Extensions.Logging;

namespace GestorGastos.Servicios
{
    public class ExtractorDocumentosMock : IExtractorDocumentos
    {
        private static readonly string[] NombresQueSimulanFalla = { "error", "borroso" };

        private readonly ILogger<ExtractorDocumentosMock> _logger;

        public ExtractorDocumentosMock(ILogger<ExtractorDocumentosMock> logger)
        {
            _logger = logger;
        }

        public Task<DatosExtraidos> ExtraerAsync(Stream contenido, TipoDocumento tipoDocumento, string nombreArchivo, CancellationToken cancelacion)
        {
            _logger.LogWarning("Extracción simulada de {NombreArchivo}: falta configurar DocumentIntelligence:Endpoint", nombreArchivo);

            var simulaFalla = NombresQueSimulanFalla.Any(n => nombreArchivo.Contains(n, StringComparison.OrdinalIgnoreCase));
            var datos = simulaFalla ? new DatosExtraidos() : DatosDeEjemplo(tipoDocumento);
            return Task.FromResult(datos);
        }

        private static DatosExtraidos DatosDeEjemplo(TipoDocumento tipoDocumento)
        {
            var esFactura = tipoDocumento == TipoDocumento.Factura;
            return new DatosExtraidos
            {
                Comercio = esFactura ? "Hotel Córdoba S.A." : "Taxi Ezeiza SRL",
                FechaComprobante = DateOnly.FromDateTime(DateTime.UtcNow.ConvertirAArgentina()),
                Descripcion = esFactura ? "Alojamiento 2 noches; Desayuno" : "Viaje al aeropuerto",
                Monto = esFactura ? 142300m : 18450m,
                Moneda = Moneda.ARS,
                ConfianzaDescripcion = 0.71,
                ConfianzaMonto = 0.95
            };
        }
    }
}
