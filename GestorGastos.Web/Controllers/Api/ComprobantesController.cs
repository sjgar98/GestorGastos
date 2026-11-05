using GestorGastos.Servicios.Interfaces;
using Microsoft.AspNetCore.Mvc;

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
    }
}