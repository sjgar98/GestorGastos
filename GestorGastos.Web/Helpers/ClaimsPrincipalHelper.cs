using System.Security.Claims;
using Microsoft.Identity.Web;

namespace GestorGastos.Web.Helpers
{
    public static class ClaimsPrincipalHelper
    {
        public static string Nombre(this ClaimsPrincipal usuario)
        {
            var nombre = usuario.FindFirstValue(ClaimConstants.Name);
            return string.IsNullOrEmpty(nombre) ? usuario.Email() : nombre;
        }

        public static string Email(this ClaimsPrincipal usuario)
        {
            return usuario.FindFirstValue(ClaimConstants.PreferredUserName)
                ?? usuario.FindFirstValue(ClaimTypes.Email)
                ?? string.Empty;
        }

        public static bool TieneRol(this ClaimsPrincipal usuario)
        {
            return usuario.IsInRole(Roles.Empleado) || usuario.IsInRole(Roles.Administrador);
        }

        public static string Rol(this ClaimsPrincipal usuario)
        {
            if (usuario.IsInRole(Roles.Administrador))
                return Roles.Administrador;

            return usuario.IsInRole(Roles.Empleado) ? Roles.Empleado : "Sin rol";
        }
    }
}
