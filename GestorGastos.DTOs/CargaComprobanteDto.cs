using GestorGastos.DTOs.Enums;

namespace GestorGastos.DTOs
{
    public class CargaComprobanteDto
    {
        public string NombreArchivo { get; set; } = string.Empty;
        public TipoDocumento TipoDocumento { get; set; }
        public Categoria Categoria { get; set; }
    }
}
