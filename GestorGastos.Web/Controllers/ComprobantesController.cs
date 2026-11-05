using GestorGastos.DTOs;
using GestorGastos.Servicios;
using GestorGastos.Servicios.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;


namespace GestorGastos.Web.Controllers
{
    public class ComprobantesController : Controller
    {
        private readonly IComprobanteServicio _comprobanteServicio;

        public ComprobantesController(IComprobanteServicio comprobanteServicio)
        {
            _comprobanteServicio = comprobanteServicio;
        }

        public async Task<IActionResult> Detalle(int id)
        {
            var usuarioId = User.GetObjectId()!;
            var resultado = await _comprobanteServicio.ObtenerDetalleAsync(id, usuarioId);

            if(!resultado.Exitoso)
            {
                return NotFound();
            }

            return View(resultado.Valor);
        }
    }
}