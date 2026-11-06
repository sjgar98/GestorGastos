using System.Globalization;

namespace GestorGastos.Web.Helpers
{
    public static class EtiquetasHelper
    {
        // Extensión para formatear montos y moneda (ej: "$ 12.345,67" o "—")
        public static string Moneda(this decimal? monto, string? monedaCodigo = "ARS")
        {
            if (!monto.HasValue) return "—";

            var culture = new CultureInfo("es-AR");
            string simbolo = monedaCodigo switch
            {
                "USD" => "US$",
                "EUR" => "€",
                "BRL" => "R$",
                "CLP" => "CLP$",
                "UYU" => "$U",
                _ => "$"
            };

            return $"{simbolo} {monto.Value.ToString("N2", culture)}";
        }

        // Extensión para formatear fecha y hora local (zona Argentina)
        public static string FechaHoraLocal(this DateTime fechaUtc)
        {
            // Opcional: conversión horaria a zona Argentina si se requiere, o formato directo
            return fechaUtc.ToString("dd/MM/yyyy HH:mm", new CultureInfo("es-AR"));
        }

        public static string FechaLocal(this DateOnly fecha)
        {
            return fecha.ToString("dd/MM/yyyy", new CultureInfo("es-AR"));
        }
    }
}