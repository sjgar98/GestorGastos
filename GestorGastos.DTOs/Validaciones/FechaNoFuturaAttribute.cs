using System.ComponentModel.DataAnnotations;
using GestorGastos.DTOs.Helpers;

namespace GestorGastos.DTOs.Validaciones
{
    public class FechaNoFuturaAttribute : ValidationAttribute
    {
        public FechaNoFuturaAttribute()
            : base("La fecha no puede ser futura.")
        {
        }

        public override bool IsValid(object? value)
        {
            if (value is not DateOnly fecha)
                return true;

            var hoyEnArgentina = FechaHelper.HoyEnArgentina();
            return fecha <= hoyEnArgentina;
        }
    }
}
