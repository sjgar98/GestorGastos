using GestorGastos.DTOs;
using GestorGastos.Servicios.Interfaces;
using GestorGastos.Web.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;

namespace GestorGastos.Web.Controllers.Api
{
    [ApiController]
    [Route("api/admin/comprobantes")]
    [Authorize(Roles = Roles.Administrador)]
    public class AdminComprobantesController : ControllerBase
    {
        private readonly IComprobanteServicio _comprobanteServicio;

        public AdminComprobantesController(IComprobanteServicio comprobanteServicio)
        {
            _comprobanteServicio = comprobanteServicio;
        }

        // POST /api/admin/comprobantes/{id}/tomar
        [HttpPost("{id}/tomar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Tomar(int id)
        {
            var adminId = User.GetObjectId();
            var resultado = await _comprobanteServicio.TomarEnRevisionAsync(id, adminId);
            return resultado.Exitoso ? Ok(resultado.Valor) : this.ComoProblema(resultado);
        }

        // POST /api/admin/comprobantes/{id}/aprobar
        [HttpPost("{id}/aprobar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Aprobar(int id)
        {
            var adminId = User.GetObjectId();
            var resultado = await _comprobanteServicio.AprobarAsync(id, adminId);
            return resultado.Exitoso ? Ok(resultado.Valor) : this.ComoProblema(resultado);
        }

        // POST /api/admin/comprobantes/{id}/rechazar
        [HttpPost("{id}/rechazar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rechazar(int id, [FromBody] RechazarComprobanteDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var adminId = User.GetObjectId();
            var resultado = await _comprobanteServicio.RechazarAsync(id, adminId, dto.Motivo);
            return resultado.Exitoso ? Ok(resultado.Valor) : this.ComoProblema(resultado);
        }

        // POST /api/admin/comprobantes/{id}/pagar
        [HttpPost("{id}/pagar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Pagar(int id, [FromBody] PagarComprobanteDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var adminId = User.GetObjectId();
            var resultado = await _comprobanteServicio.MarcarPagadoAsync(id, adminId, dto.FechaPago!.Value);
            return resultado.Exitoso ? Ok(resultado.Valor) : this.ComoProblema(resultado);
        }
    }
}
