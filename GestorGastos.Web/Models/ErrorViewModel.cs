namespace GestorGastos.Web.Models
{
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }

        public bool MostrarRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
