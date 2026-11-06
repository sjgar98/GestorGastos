using GestorGastos.Servicios;
using Microsoft.AspNetCore.Mvc;

namespace GestorGastos.Web.Helpers
{
    public static class ResultadoMvcHelper
    {
        public static IActionResult ComoVista<T>(this Controller controller, Resultado<T> resultado)
        {
            return resultado.Error switch
            {
                TipoError.NoEncontrado => controller.NotFound(),
                TipoError.Prohibido => controller.Forbid(),
                _ => controller.BadRequest()
            };
        }
    }
}