using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GestorGastos.DTOs;
using GestorGastos.Servicios.Interfaces;


using GestorGastos.Servicios.Interfaces;
using GestorGastos.Web.Helpers;
using GestorGastos.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;

namespace GestorGastos.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Administrador")]
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
            // Obtiene los comprobantes filtrados, ordenados y paginados para admin
            var resultado = await _comprobanteServicio.ListarAdminAsync(filtro);

            // Carga la lista de empleados para poblar el menú desplegable de filtros
            ViewBag.Empleados = await _usuarioServicio.ListarEmpleadosAsync();
            ViewBag.Filtro = filtro;

            return View(resultado);
        }

        [HttpGet]
        
        public async Task<IActionResult> Detalle(int id)
        {
            // El servicio se encarga de buscar la lógica y retornar directamente el DTO que espera la vista
            var dto = await _comprobanteServicio.ObtenerDetalleAdminAsync(id);

            if (dto == null)
            {
                return NotFound();
            }

            return View(dto);
        }
    }
}
    [Authorize(Roles = Roles.Administrador)]
    public class ComprobantesController : Controller
    {
        private readonly IComprobanteServicio _comprobanteServicio;

        public ComprobantesController(IComprobanteServicio comprobanteServicio)
        {
            _comprobanteServicio = comprobanteServicio;
        }

        // GET /Admin/Comprobantes
        // TODO P6 (Federico): implementar el listado de todos los comprobantes
        public IActionResult Index()
        {
            return View();
        }

        // GET /Admin/Comprobantes/Detalle/{id}
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
