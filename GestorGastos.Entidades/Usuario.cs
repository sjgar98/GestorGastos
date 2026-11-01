namespace GestorGastos.Entidades
{
    public class Usuario
    {
        public string Id { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime UltimoAcceso { get; set; }

        public List<Comprobante> Comprobantes { get; set; } = new List<Comprobante>();
    }
}
