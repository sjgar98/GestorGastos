namespace GestorGastos.DTOs
{
    public class UrlArchivoDto
    {
        public string Url { get; set; } = string.Empty;
        public DateTime Vencimiento { get; set; }
        public string ContentType { get; set; } = string.Empty;
    }
}