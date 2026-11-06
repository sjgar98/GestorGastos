using System.ComponentModel.DataAnnotations;
using GestorGastos.DTOs.Enums;

namespace GestorGastos.Web.Models
{
    public class CargarComprobanteViewModel
    {
        [Required(ErrorMessage = "Seleccioná un archivo.")]
        public IFormFile? Archivo { get; set; }

        [Required(ErrorMessage = "Elegí el tipo de documento.")]
        public TipoDocumento? TipoDocumento { get; set; }

        [Required(ErrorMessage = "Elegí una categoría.")]
        public Categoria? Categoria { get; set; }
    }
}
