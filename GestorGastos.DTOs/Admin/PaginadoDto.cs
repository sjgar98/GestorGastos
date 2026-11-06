namespace GestorGastos.DTOs
{
    public class PaginadoDto<T>
    {
        public List<T> Elementos { get; set; } = new List<T>();
        public int TotalRegistros { get; set; }
        public int PaginaActual { get; set; }
        public int TamanoPagina { get; set; }
        public int TotalPaginas => (int)Math.Ceiling((double)TotalRegistros / TamanoPagina);
    }
}