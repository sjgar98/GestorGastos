using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GestorGastos.DTOs;
using GestorGastos.Servicios.Interfaces;
using GestorGastos.Web.Helpers;
using GestorGastos.Web.Models;
using Microsoft.Identity.Web;

namespace GestorGastos.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = Roles.Administrador)]
    public class ComprobantesController : Controller
    {
        private readonly IComprobanteServicio _comprobanteServicio;
        private readonly IUsuarioServicio _usuarioServicio;

        public ComprobantesController(IComprobanteServicio comprobanteServicio, IUsuarioServicio usuarioServicio)
        {
            _comprobanteServicio = comprobanteServicio;
            _usuarioServicio = usuarioServicio;
        }

        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] FiltroAdminComprobantesDto filtro)
        {
            var resultado = await _comprobanteServicio.ListarAdminAsync(filtro);
            ViewBag.Empleados = await _usuarioServicio.ListarEmpleadosAsync();
            ViewBag.Filtro = filtro;

            return View(resultado);
        }

        [HttpGet]
        public async Task<IActionResult> Detalle(int id)
        {
            var resultado = await _comprobanteServicio.ObtenerAdminDetalleAsync(id);

            if (!resultado.Exitoso)
                return this.ComoVista(resultado);

            var vm = new AdminDetalleViewModel
            {
                Comprobante = resultado.Valor!,
                EsPropio = resultado.Valor!.EmpleadoId == User.GetObjectId()
            };

            return View(vm);
        }
    }
}