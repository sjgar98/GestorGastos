using GestorGastos.Servicios.Interfaces;
using Microsoft.Azure.Functions.Worker;

namespace GestorGastos.Functions
{
    public class ExtraerComprobanteFunction
    {
        // Azurite no emite eventos de Event Grid y Flex Consumption solo admite Event Grid:
        // el origen del trigger se fija en compilación porque el atributo necesita una constante.
#if DEBUG
        private const BlobTriggerSource OrigenTrigger = BlobTriggerSource.LogsAndContainerScan;
#else
        private const BlobTriggerSource OrigenTrigger = BlobTriggerSource.EventGrid;
#endif

        private readonly IExtraccionServicio _extraccionServicio;

        public ExtraerComprobanteFunction(IExtraccionServicio extraccionServicio)
        {
            _extraccionServicio = extraccionServicio;
        }

        [Function(nameof(ExtraerComprobanteFunction))]
        public async Task EjecutarAsync(
            [BlobTrigger("comprobantes/{name}", Source = OrigenTrigger, Connection = "Storage:ConnectionString")] Stream contenido,
            string name,
            CancellationToken cancelacion)
        {
            await _extraccionServicio.ProcesarAsync(name, contenido, cancelacion);
        }
    }
}
