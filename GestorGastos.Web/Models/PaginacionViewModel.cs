using GestorGastos.DTOs;

namespace GestorGastos.Web.Models
{
    public class PaginacionViewModel
    {
        public IPaginado Paginado { get; set; } = null!;
        public Dictionary<string, string> Filtros { get; set; } = new Dictionary<string, string>();
    }
}
