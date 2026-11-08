namespace GestorGastos.DTOs
{
    public class ResumenDashboardDto
    {
        public int DocumentosAbiertos { get; set; }
        public int ErroresExtraccion { get; set; }
        public List<ConteoEstadoDto> PorEstado { get; set; } = new List<ConteoEstadoDto>();
    }
}
