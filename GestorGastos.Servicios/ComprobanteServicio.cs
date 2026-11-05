using GestorGastos.Repositorios.Interfaces;
using GestorGastos.Servicios.Interfaces;

namespace GestorGastos.Servicios
{
    public class ComprobanteServicio : IComprobanteServicio
    {
        private readonly IComprobanteRepositorio _comprobanteRepositorio;

        public ComprobanteServicio(IComprobanteRepositorio comprobanteRepositorio)
        {
            _comprobanteRepositorio = comprobanteRepositorio;
        }
    }
}