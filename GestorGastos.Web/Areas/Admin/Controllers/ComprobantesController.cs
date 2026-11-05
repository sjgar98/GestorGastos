using GestorGastos.Servicios.Interfaces;
using GestorGastos.Web.Helpers;
using GestorGastos.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;

namespace GestorGastos.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
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
}
