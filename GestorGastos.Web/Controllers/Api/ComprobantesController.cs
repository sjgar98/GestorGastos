using GestorGastos.Servicios.Interfaces;
using GestorGastos.Web.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;

namespace GestorGastos.Web.Controllers.Api
{
    [ApiController]
    [Route("api/comprobantes")]
    public class ComprobantesController : ControllerBase
    {
        private readonly IComprobanteServicio _comprobanteServicio;

        public ComprobantesController(IComprobanteServicio comprobanteServicio)
        {
            _comprobanteServicio = comprobanteServicio;
        }

        [HttpGet("{id:int}/estado")]
        public async Task<IActionResult> ObtenerEstado(int id)
        {
            var resultado = await _comprobanteServicio.ObtenerEstadoAsync(id, User.GetObjectId()!);
            return resultado.Exitoso ? Ok(resultado.Valor) : this.ComoProblema(resultado);
        }

        [HttpDelete("{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(int id)
        {
            var resultado = await _comprobanteServicio.EliminarAsync(id, User.GetObjectId()!);
            return resultado.Exitoso ? NoContent() : this.ComoProblema(resultado);
        }
    }
}
