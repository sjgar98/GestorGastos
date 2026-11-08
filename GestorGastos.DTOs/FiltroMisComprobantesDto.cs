namespace GestorGastos.DTOs
{
    public class FiltroMisComprobantesDto
    {
        public string? Estado { get; set; }
        public string? Orden { get; set; } = "recientes";
        public int Pagina { get; set; } = 1;
    }
}
