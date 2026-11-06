using GestorGastos.DTOs;

namespace GestorGastos.Servicios.Interfaces
{
    public interface IAlmacenamientoArchivos
    {
        Task SubirAsync(string blobNombre, Stream contenido, string contentType);
        Task EliminarAsync(string blobNombre);
        Task<UrlArchivoDto> GenerarUrlLecturaAsync(string blobNombre, string? nombreDescarga = null);
    }
}