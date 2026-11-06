using GestorGastos.DTOs;
using GestorGastos.DTOs.Enums;
using GestorGastos.Servicios;
using GestorGastos.Servicios.Interfaces;
using GestorGastos.Web.Helpers;
using GestorGastos.Web.Models;
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

        [HttpGet]
        public IActionResult Cargar()
        {
            return View(new CargarComprobanteViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cargar(CargarComprobanteViewModel modelo)
        {
            if (!ModelState.IsValid)
                return View(modelo);

            var datos = new CargaComprobanteDto
            {
                NombreArchivo = modelo.Archivo!.FileName,
                TipoDocumento = modelo.TipoDocumento!.Value,
                Categoria = modelo.Categoria!.Value
            };

            await using var contenido = modelo.Archivo.OpenReadStream();
            var resultado = await _comprobanteServicio.CargarAsync(User.GetObjectId()!, datos, contenido);
            if (!resultado.Exitoso)
            {
                ModelState.AddModelError(nameof(modelo.Archivo), resultado.Mensaje!);
                return View(modelo);
            }

            if (resultado.Valor!.SoloSeAnalizanDosPaginas)
                TempData["AvisoPaginas"] = "Solo se analizarán las primeras 2 páginas.";

            return RedirectToAction(nameof(Resultado), new { id = resultado.Valor.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Resultado(int id)
        {
            var resultado = await _comprobanteServicio.ObtenerEstadoAsync(id, User.GetObjectId()!);
            if (!resultado.Exitoso)
                return this.ComoVista(resultado);

            return View(new ResultadoCargaViewModel
            {
                Comprobante = resultado.Valor!,
                Formulario = AFormulario(resultado.Valor!)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, [Bind(Prefix = nameof(ResultadoCargaViewModel.Formulario))] EditarComprobanteDto formulario)
        {
            var usuarioId = User.GetObjectId()!;
            var estadoPrevio = await _comprobanteServicio.ObtenerEstadoAsync(id, usuarioId);
            if (!estadoPrevio.Exitoso)
                return this.ComoVista(estadoPrevio);

            if (!ModelState.IsValid)
                return VistaResultado(estadoPrevio.Valor!, formulario);

            var resultado = await _comprobanteServicio.EditarAsync(id, usuarioId, formulario);
            if (resultado.Error == TipoError.NoEncontrado)
                return this.ComoVista(resultado);

            if (!resultado.Exitoso)
            {
                ModelState.AddModelError(string.Empty, resultado.Mensaje!);
                return VistaResultado(estadoPrevio.Valor!, formulario);
            }

            TempData["MensajeExito"] = estadoPrevio.Valor!.Estado == EstadoComprobante.ErrorExtraccion
                ? "Datos guardados. El comprobante quedó Pendiente de Revisión."
                : "Cambios guardados.";

            return RedirectToAction(nameof(Resultado), new { id });
        }

        private ViewResult VistaResultado(EstadoComprobanteDto comprobante, EditarComprobanteDto formulario)
        {
            return View(nameof(Resultado), new ResultadoCargaViewModel
            {
                Comprobante = comprobante,
                Formulario = formulario
            });
        }

        private static EditarComprobanteDto AFormulario(EstadoComprobanteDto comprobante)
        {
            return new EditarComprobanteDto
            {
                Comercio = comprobante.Comercio,
                FechaComprobante = comprobante.FechaComprobante,
                Descripcion = comprobante.Descripcion,
                Monto = comprobante.Monto,
                Moneda = comprobante.Moneda ?? Moneda.ARS,
                Categoria = comprobante.Categoria
            };
        }
    }
}
