using Microsoft.Data.SqlClient;

namespace GestorGastos.Entidades
{
    public static class ConexionBaseDatos
    {
        // En Azure la cadena viene de la configuración. En local queda vacía y se usa LocalDB (Windows)
        // o, si está definida GESTORGASTOS_DB_HOST, el SQL Server de Docker (Linux/macOS, ver .env.example).
        public static string Obtener(string? configurada)
        {
            if (!string.IsNullOrEmpty(configurada))
                return configurada;

            var host = Environment.GetEnvironmentVariable("GESTORGASTOS_DB_HOST");
            if (string.IsNullOrEmpty(host))
            {
                if (!OperatingSystem.IsWindows())
                    throw new InvalidOperationException("Falta ConnectionStrings:GestorGastos o la variable de entorno GESTORGASTOS_DB_HOST (LocalDB solo existe en Windows; ver .env.example).");

                return @"Server=(localdb)\MSSQLLocalDB;Database=GestorGastos;Trusted_Connection=True;TrustServerCertificate=True";
            }

            var puerto = Environment.GetEnvironmentVariable("GESTORGASTOS_DB_PORT") ?? "1433";
            var conexion = new SqlConnectionStringBuilder
            {
                DataSource = $"{host},{puerto}",
                InitialCatalog = Environment.GetEnvironmentVariable("GESTORGASTOS_DB_NAME") ?? "GestorGastos",
                UserID = ObtenerRequerida("GESTORGASTOS_DB_USER"),
                Password = ObtenerRequerida("GESTORGASTOS_DB_PASSWORD"),
                TrustServerCertificate = true
            };
            return conexion.ConnectionString;
        }

        private static string ObtenerRequerida(string variable) =>
            Environment.GetEnvironmentVariable(variable)
            ?? throw new InvalidOperationException($"Falta la variable de entorno {variable} (requerida cuando se define GESTORGASTOS_DB_HOST).");
    }
}
