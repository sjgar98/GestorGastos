using GestorGastos.Servicios.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GestorGastos.Web.Controllers
{
    public class ComprobantesController : Controller
    {
        private readonly IComprobanteServicio _comprobanteServicio;

        public ComprobantesController(IComprobanteServicio comprobanteServicio)
        {
            _comprobanteServicio = comprobanteServicio;
        }
    }
}