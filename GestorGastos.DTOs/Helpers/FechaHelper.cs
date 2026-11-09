namespace GestorGastos.DTOs.Helpers
{
    public static class FechaHelper
    {
        private static readonly TimeZoneInfo _zonaArgentina =
            TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Buenos_Aires");

        public static DateTime ConvertirAArgentina(this DateTime fechaUtc)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.SpecifyKind(fechaUtc, DateTimeKind.Utc),
                _zonaArgentina);
        }

        public static DateOnly HoyEnArgentina()
        {
            return DateOnly.FromDateTime(DateTime.UtcNow.ConvertirAArgentina());
        }

        public static DateTime InicioDelDiaArgentinaEnUtc(this DateOnly fecha)
        {
            return TimeZoneInfo.ConvertTimeToUtc(
                fecha.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified),
                _zonaArgentina);
        }

        public static string FechaLocal(this DateOnly? fecha)
        {
            return fecha?.ToString("dd/MM/yyyy") ?? "—";
        }

        public static string FechaLocal(this DateTime fechaUtc)
        {
            return fechaUtc.ConvertirAArgentina().ToString("dd/MM/yyyy");
        }

        public static string FechaHoraLocal(this DateTime fechaUtc)
        {
            return fechaUtc.ConvertirAArgentina().ToString("dd/MM/yyyy HH:mm");
        }
    }
}
