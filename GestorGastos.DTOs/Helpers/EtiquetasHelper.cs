using System.Globalization;
using GestorGastos.DTOs.Enums;

namespace GestorGastos.DTOs.Helpers
{
    public static class EtiquetasHelper
    {
        private static readonly CultureInfo CulturaArgentina = CultureInfo.GetCultureInfo("es-AR");

        public static string Texto(this EstadoComprobante estado) => estado switch
        {
            EstadoComprobante.Procesando => "Procesando",
            EstadoComprobante.ErrorExtraccion => "Error de extracción",
            EstadoComprobante.PendienteRevision => "Pendiente de Revisión",
            EstadoComprobante.EnRevision => "En Revisión",
            EstadoComprobante.Aprobado => "Aprobado",
            EstadoComprobante.Rechazado => "Rechazado",
            EstadoComprobante.Pagado => "Pagado",
            _ => estado.ToString()
        };

        public static string ClaseBadge(this EstadoComprobante estado) => estado switch
        {
            EstadoComprobante.Procesando => "badge text-bg-secondary",
            EstadoComprobante.ErrorExtraccion => "badge bg-danger-subtle text-danger-emphasis border border-danger-subtle",
            EstadoComprobante.PendienteRevision => "badge text-bg-warning",
            EstadoComprobante.EnRevision => "badge text-bg-info",
            EstadoComprobante.Aprobado => "badge text-bg-primary",
            EstadoComprobante.Rechazado => "badge text-bg-danger",
            EstadoComprobante.Pagado => "badge text-bg-success",
            _ => "badge text-bg-secondary"
        };

        public static string Texto(this Categoria categoria) => categoria switch
        {
            Categoria.Viaticos => "Viáticos",
            Categoria.Pasajes => "Pasajes",
            Categoria.Hospedaje => "Hospedaje",
            Categoria.Traslados => "Traslados",
            Categoria.Otros => "Otros",
            _ => categoria.ToString()
        };

        public static string Texto(this TipoDocumento tipo) => tipo switch
        {
            TipoDocumento.Recibo => "Recibo",
            TipoDocumento.Factura => "Factura",
            _ => tipo.ToString()
        };

        public static string Moneda(this decimal? monto, Moneda? moneda)
        {
            if (monto is null) return "—";
            var simbolo = moneda switch
            {
                Enums.Moneda.USD => "USD",
                Enums.Moneda.EUR => "€",
                Enums.Moneda.BRL => "R$",
                Enums.Moneda.CLP => "CLP",
                Enums.Moneda.UYU => "UYU",
                _ => "$"
            };
            return $"{simbolo} {monto.Value.ToString("N2", CulturaArgentina)}";
        }
    }
}