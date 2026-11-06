using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using GestorGastos.DTOs;
using GestorGastos.Servicios.Interfaces;

namespace GestorGastos.Servicios
{
    public class AlmacenamientoArchivos : IAlmacenamientoArchivos
    {
        private static readonly TimeSpan VigenciaUrl = TimeSpan.FromMinutes(5);

        private readonly BlobContainerClient _contenedor;

        public AlmacenamientoArchivos(string connectionString, string nombreContenedor)
        {
            if (string.IsNullOrWhiteSpace(connectionString) || string.IsNullOrWhiteSpace(nombreContenedor))
            {
                throw new InvalidOperationException(
                    "Falta configurar Storage:ConnectionString y Storage:Contenedor.");
            }

            _contenedor = new BlobContainerClient(connectionString, nombreContenedor);
        }

        public async Task SubirAsync(string blobNombre, Stream contenido, string contentType)
        {
            await _contenedor.CreateIfNotExistsAsync();

            var opciones = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
            };

            await _contenedor.GetBlobClient(blobNombre).UploadAsync(contenido, opciones);
        }

        public async Task EliminarAsync(string blobNombre)
        {
            await _contenedor.GetBlobClient(blobNombre).DeleteIfExistsAsync();
        }

        public Task<UrlArchivoDto> GenerarUrlLecturaAsync(string blobNombre, string? nombreDescarga = null)
        {
            var blob = _contenedor.GetBlobClient(blobNombre);

            // Firmar un SAS exige que la connection string incluya la clave de la cuenta.
            if (!blob.CanGenerateSasUri)
            {
                throw new InvalidOperationException(
                    "El almacenamiento no puede generar URLs SAS: la connection string debe incluir la clave de la cuenta.");
            }

            var vencimiento = DateTimeOffset.UtcNow.Add(VigenciaUrl);
            var sas = new BlobSasBuilder(BlobSasPermissions.Read, vencimiento)
            {
                BlobContainerName = _contenedor.Name,
                BlobName = blobNombre,
                Resource = "b"
            };

            if (!string.IsNullOrWhiteSpace(nombreDescarga))
            {
                var nombreSeguro = nombreDescarga.Replace("\"", string.Empty);
                sas.ContentDisposition =
                    $"attachment; filename=\"{nombreSeguro}\"; filename*=UTF-8''{Uri.EscapeDataString(nombreDescarga)}";
            }

            var url = new UrlArchivoDto
            {
                Url = blob.GenerateSasUri(sas).ToString(),
                Vencimiento = vencimiento.UtcDateTime
            };

            return Task.FromResult(url);
        }
    }
}