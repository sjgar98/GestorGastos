using System.Text;
using System.Text.RegularExpressions;

namespace GestorGastos.Servicios
{
    public static class ValidadorArchivo
    {
        public const long TamanioMaximoBytes = 4 * 1024 * 1024;
        public const int PaginasAnalizadas = 2;

        private static readonly Dictionary<string, (string ContentType, byte[] Firma)> FormatosPermitidos = new()
        {
            [".pdf"] = ("application/pdf", new byte[] { 0x25, 0x50, 0x44, 0x46 }),
            [".jpg"] = ("image/jpeg", new byte[] { 0xFF, 0xD8, 0xFF }),
            [".jpeg"] = ("image/jpeg", new byte[] { 0xFF, 0xD8, 0xFF }),
            [".png"] = ("image/png", new byte[] { 0x89, 0x50, 0x4E, 0x47 })
        };

        private static readonly Regex PaginaPdf = new(@"/Type\s*/Page(?![a-zA-Z])", RegexOptions.Compiled);

        public static Resultado<ArchivoValidado> Validar(string nombreArchivo, byte[] contenido)
        {
            if (contenido.Length == 0)
                return Resultado<ArchivoValidado>.Fallo(TipoError.Invalido, "Seleccioná un archivo.");

            var extension = Path.GetExtension(nombreArchivo).ToLowerInvariant();
            if (!FormatosPermitidos.TryGetValue(extension, out var formato))
                return Resultado<ArchivoValidado>.Fallo(TipoError.Invalido, "Formato no permitido. Usá PDF, JPG o PNG.");

            if (contenido.Length > TamanioMaximoBytes)
                return Resultado<ArchivoValidado>.Fallo(TipoError.Invalido, "El archivo supera los 4 MB.");

            if (!contenido.AsSpan().StartsWith(formato.Firma))
                return Resultado<ArchivoValidado>.Fallo(TipoError.Invalido, "El contenido del archivo no corresponde a su formato.");

            return Resultado<ArchivoValidado>.Exito(new ArchivoValidado
            {
                Extension = extension,
                ContentType = formato.ContentType,
                CantidadPaginas = formato.ContentType == "application/pdf" ? ContarPaginasPdf(contenido) : 1
            });
        }

        // Estimación: cuenta los objetos /Type /Page sin descomprimir el PDF; si las páginas están
        // dentro de object streams comprimidos no se ven y el aviso de RN-06 no aparece.
        private static int ContarPaginasPdf(byte[] contenido)
        {
            var texto = Encoding.Latin1.GetString(contenido);
            return PaginaPdf.Matches(texto).Count;
        }
    }
}
