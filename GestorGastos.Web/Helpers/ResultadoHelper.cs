using GestorGastos.Servicios;
using Microsoft.AspNetCore.Mvc;

namespace GestorGastos.Web.Helpers
{
    public static class ResultadoHelper
    {
        public static ObjectResult ComoProblema<T>(this ControllerBase controller, Resultado<T> resultado)
        {
            var error = resultado.Error
                ?? throw new InvalidOperationException("Un resultado exitoso no se puede traducir a un error.");

            var (codigo, titulo) = error switch
            {
                TipoError.NoEncontrado => (StatusCodes.Status404NotFound, "No encontrado"),
                TipoError.Prohibido => (StatusCodes.Status403Forbidden, "Sin permiso"),
                TipoError.Conflicto => (StatusCodes.Status409Conflict, "Conflicto"),
                _ => (StatusCodes.Status400BadRequest, "Datos inválidos")
            };

            return controller.Problem(detail: resultado.Mensaje, statusCode: codigo, title: titulo);
        }
    }
}