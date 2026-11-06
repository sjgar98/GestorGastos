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

        // GET /Admin/Comprobantes (P6)
        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] FiltroAdminComprobantesDto filtro)
        {
            // Obtiene los comprobantes filtrados, ordenados y paginados para admin
            var resultado = await _comprobanteServicio.ListarAdminAsync(filtro);

            // Carga la lista de empleados para poblar el menú desplegable de filtros
            ViewBag.Empleados = await _usuarioServicio.ListarEmpleadosAsync();
            ViewBag.Filtro = filtro;

            return View(resultado);
        }

        // GET /Admin/Comprobantes/Detalle/{id} (P7)
        [HttpGet]
        public async Task<IActionResult> Detalle(int id)
        {
            var adminId = User.GetObjectId();
            var resultado = await _comprobanteServicio.ObtenerAdminDetalleAsync(id, adminId);

            if (!resultado.Exitoso)
                return this.ComoVista(resultado);

            var vm = new AdminDetalleViewModel
            {
                Comprobante = resultado.Valor!,
                EsPropio = resultado.Valor!.EmpleadoId == adminId
            };

            return View(vm);
        }
    }
}