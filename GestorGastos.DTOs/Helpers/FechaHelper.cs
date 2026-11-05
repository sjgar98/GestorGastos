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
    }
}