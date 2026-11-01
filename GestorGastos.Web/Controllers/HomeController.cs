using System.Diagnostics;
using GestorGastos.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestorGastos.Web.Controllers
{
    public class HomeController : Controller
    {
        // TODO Autenticación: sin sesión devolver la vista Bienvenida (P1) y con sesión el Dashboard (P2).
        [AllowAnonymous]
        public IActionResult Index()
        {
            return View();
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
