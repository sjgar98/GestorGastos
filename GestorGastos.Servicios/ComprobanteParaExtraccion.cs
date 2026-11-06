using GestorGastos.DTOs.Enums;

namespace GestorGastos.Servicios
{
    public class ComprobanteParaExtraccion
    {
        public int Id { get; set; }
        public string NombreArchivoOriginal { get; set; } = string.Empty;
        public TipoDocumento TipoDocumento { get; set; }
    }
}
