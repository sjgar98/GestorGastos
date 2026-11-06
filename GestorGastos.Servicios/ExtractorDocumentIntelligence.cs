using Azure;
using Azure.AI.DocumentIntelligence;
using GestorGastos.DTOs.Enums;
using GestorGastos.Servicios.Interfaces;

namespace GestorGastos.Servicios
{
    public class ExtractorDocumentIntelligence : IExtractorDocumentos
    {
        private static readonly CamposModelo CamposRecibo = new("prebuilt-receipt", "MerchantName", "TransactionDate", "Total");
        private static readonly CamposModelo CamposFactura = new("prebuilt-invoice", "VendorName", "InvoiceDate", "InvoiceTotal");

        private const string CampoItems = "Items";
        private const string CampoDescripcionItem = "Description";
        private const string SeparadorItems = "; ";

        private readonly DocumentIntelligenceClient _cliente;

        public ExtractorDocumentIntelligence(string endpoint, string clave)
        {
            if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(clave))
            {
                throw new InvalidOperationException(
                    "Falta configurar DocumentIntelligence:Endpoint y DocumentIntelligence:Key.");
            }

            _cliente = new DocumentIntelligenceClient(new Uri(endpoint), new AzureKeyCredential(clave));
        }

        public async Task<DatosExtraidos> ExtraerAsync(Stream contenido, TipoDocumento tipoDocumento, string nombreArchivo, CancellationToken cancelacion)
        {
            var campos = tipoDocumento == TipoDocumento.Factura ? CamposFactura : CamposRecibo;
            var archivo = await BinaryData.FromStreamAsync(contenido, cancelacion);

            var operacion = await _cliente.AnalyzeDocumentAsync(
                WaitUntil.Completed, new AnalyzeDocumentOptions(campos.Modelo, archivo), cancelacion);

            var documento = operacion.Value.Documents.FirstOrDefault();
            return documento is null ? new DatosExtraidos() : ADatosExtraidos(documento, campos);
        }

        private static DatosExtraidos ADatosExtraidos(AnalyzedDocument documento, CamposModelo campos)
        {
            var monto = ObtenerCampo(documento, campos.Monto);
            var descripciones = ObtenerDescripcionesDeItems(documento);

            return new DatosExtraidos
            {
                Comercio = ObtenerCampo(documento, campos.Comercio)?.ValueString,
                FechaComprobante = AFecha(ObtenerCampo(documento, campos.Fecha)),
                Descripcion = string.Join(SeparadorItems, descripciones.Select(d => d.ValueString ?? d.Content)),
                Monto = AMonto(monto),
                Moneda = AMoneda(monto),
                ConfianzaDescripcion = descripciones.Count == 0 ? null : descripciones.Min(d => d.Confidence),
                ConfianzaMonto = monto?.Confidence
            };
        }

        private static DocumentField? ObtenerCampo(AnalyzedDocument documento, string nombre)
        {
            return documento.Fields.TryGetValue(nombre, out var campo) ? campo : null;
        }

        private static List<DocumentField> ObtenerDescripcionesDeItems(AnalyzedDocument documento)
        {
            var items = ObtenerCampo(documento, CampoItems)?.ValueList;
            if (items is null)
                return new List<DocumentField>();

            return items
                .Where(item => item.ValueDictionary is not null && item.ValueDictionary.ContainsKey(CampoDescripcionItem))
                .Select(item => item.ValueDictionary[CampoDescripcionItem])
                .ToList();
        }

        private static DateOnly? AFecha(DocumentField? campo)
        {
            return campo?.ValueDate is { } fecha ? DateOnly.FromDateTime(fecha.Date) : null;
        }

        // Según la versión del modelo, el total llega como moneda o como número sin moneda.
        private static decimal? AMonto(DocumentField? campo)
        {
            var monto = campo?.ValueCurrency?.Amount ?? campo?.ValueDouble;
            return monto is null ? null : Convert.ToDecimal(monto.Value);
        }

        private static Moneda? AMoneda(DocumentField? campo)
        {
            var codigo = campo?.ValueCurrency?.CurrencyCode;
            return Enum.GetNames<Moneda>().Contains(codigo, StringComparer.OrdinalIgnoreCase)
                ? Enum.Parse<Moneda>(codigo!, ignoreCase: true)
                : null;
        }

        private sealed record CamposModelo(string Modelo, string Comercio, string Fecha, string Monto);
    }
}
