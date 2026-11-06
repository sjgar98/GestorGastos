using GestorGastos.DTOs.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace GestorGastos.DTOs.Admin
{

    public class HistorialItemDto
    {
        public EstadoComprobante? EstadoAnterior { get; set; }
        public EstadoComprobante EstadoNuevo { get; set; }
        public string? NombreUsuario { get; set; }
        public DateTime Fecha { get; set; }
        public string? Observacion { get; set; }
    }
}
