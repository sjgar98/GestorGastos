using Microsoft.AspNetCore.Mvc;

namespace GestorGastos.Web.Helpers
{
    public static class ProblemaApiHelper
    {
        public static bool EsRutaApi(this HttpRequest request)
        {
            return request.Path.StartsWithSegments("/api");
        }

        public static async Task EscribirProblemaAsync(this HttpContext contexto, int codigo, string titulo, string detalle)
        {
            contexto.Response.StatusCode = codigo;
            var servicio = contexto.RequestServices.GetRequiredService<IProblemDetailsService>();
            await servicio.WriteAsync(new ProblemDetailsContext
            {
                HttpContext = contexto,
                ProblemDetails = new ProblemDetails { Status = codigo, Title = titulo, Detail = detalle }
            });
        }
    }
}
