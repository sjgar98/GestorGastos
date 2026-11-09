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
        public async Task<IActionResult> Index([FromQuery] FiltroMisComprobantesDto filtro)
        {
            ViewBag.Filtro = filtro;
            var listado = await _comprobanteServicio.ListarMisComprobantesAsync(User.GetObjectId()!, filtro);
            return View(listado);
        }

        public async Task<IActionResult> Detalle(int id)
        {
            var resultado = await _comprobanteServicio.ObtenerDetalleAsync(id, User.GetObjectId()!);
            if (!resultado.Exitoso)
                return this.ComoVista(resultado);

            var comprobante = resultado.Valor!;
            return View(new DetalleComprobanteViewModel
            {
                Comprobante = comprobante,
                Formulario = AFormulario(comprobante.Comercio, comprobante.FechaComprobante, comprobante.Descripcion,
                    comprobante.Monto, comprobante.Moneda, comprobante.Categoria)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarDetalle(int id, [Bind(Prefix = nameof(DetalleComprobanteViewModel.Formulario))] EditarComprobanteDto formulario)
        {
            var usuarioId = User.GetObjectId()!;

            if (!ModelState.IsValid)
            {
                return await VistaDetalleConFormularioAsync(id, usuarioId, formulario);
            }

            var resultado = await _comprobanteServicio.EditarAsync(id, usuarioId, formulario);
            if (resultado.Error == TipoError.NoEncontrado)
                return this.ComoVista(resultado);

            if (!resultado.Exitoso)
            {
                ModelState.AddModelError(string.Empty, resultado.Mensaje!);
                return await VistaDetalleConFormularioAsync(id, usuarioId, formulario);
            }

            TempData["MensajeExito"] = "Cambios guardados.";
            return RedirectToAction(nameof(Detalle), new { id });
        }

        private async Task<IActionResult> VistaDetalleConFormularioAsync(int id, string usuarioId, EditarComprobanteDto formulario)
        {
            var detalle = await _comprobanteServicio.ObtenerDetalleAsync(id, usuarioId);
            if (!detalle.Exitoso)
                return this.ComoVista(detalle);

            return View(nameof(Detalle), new DetalleComprobanteViewModel
            {
                Comprobante = detalle.Valor!,
                Formulario = formulario
            });
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

            var comprobante = resultado.Valor!;
            return View(new ResultadoCargaViewModel
            {
                Comprobante = comprobante,
                Formulario = AFormulario(comprobante.Comercio, comprobante.FechaComprobante, comprobante.Descripcion,
                    comprobante.Monto, comprobante.Moneda, comprobante.Categoria)
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

            return RedirectToAction(nameof(Index));
        }

        private ViewResult VistaResultado(EstadoComprobanteDto comprobante, EditarComprobanteDto formulario)
        {
            return View(nameof(Resultado), new ResultadoCargaViewModel
            {
                Comprobante = comprobante,
                Formulario = formulario
            });
        }

        private static EditarComprobanteDto AFormulario(string? comercio, DateOnly? fecha, string? descripcion,
            decimal? monto, Moneda? moneda, Categoria categoria)
        {
            return new EditarComprobanteDto
            {
                Comercio = comercio,
                FechaComprobante = fecha,
                Descripcion = descripcion,
                Monto = monto,
                Moneda = moneda ?? Moneda.ARS,
                Categoria = categoria
            };
        }
    }
}