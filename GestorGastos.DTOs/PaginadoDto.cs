namespace GestorGastos.DTOs
{
    public interface IPaginado
    {
        int TotalRegistros { get; }
        int PaginaActual { get; }
        int TotalPaginas { get; }
        int Desde { get; }
        int Hasta { get; }
    }

    public class PaginadoDto<T> : IPaginado
    {
        public List<T> Elementos { get; set; } = new List<T>();
        public int TotalRegistros { get; set; }
        public int PaginaActual { get; set; }
        public int TamanoPagina { get; set; }
        public int TotalPaginas => (int)Math.Ceiling((double)TotalRegistros / TamanoPagina);
        public int Desde => TotalRegistros == 0 ? 0 : ((PaginaActual - 1) * TamanoPagina) + 1;
        public int Hasta => Math.Min(PaginaActual * TamanoPagina, TotalRegistros);
    }
}
