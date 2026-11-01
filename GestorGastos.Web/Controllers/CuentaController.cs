using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestorGastos.Web.Controllers
{
    public class CuentaController : Controller
    {
        [AllowAnonymous]
        public IActionResult IniciarSesion()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            var propiedades = new AuthenticationProperties { RedirectUri = Url.Action("Index", "Home") };
            return Challenge(propiedades, OpenIdConnectDefaults.AuthenticationScheme);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CerrarSesion()
        {
            var propiedades = new AuthenticationProperties { RedirectUri = Url.Action("Index", "Home") };
            return SignOut(propiedades, CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme);
        }

        [AllowAnonymous]
        public IActionResult AccesoDenegado()
        {
            Response.StatusCode = StatusCodes.Status403Forbidden;
            return View();
        }
    }
}
