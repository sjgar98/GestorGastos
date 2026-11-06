using GestorGastos.DTOs.Enums;

namespace GestorGastos.Servicios.Interfaces
{
    public interface IExtractorDocumentos
    {
        Task<DatosExtraidos> ExtraerAsync(Stream contenido, TipoDocumento tipoDocumento, string nombreArchivo, CancellationToken cancelacion);
    }
}
