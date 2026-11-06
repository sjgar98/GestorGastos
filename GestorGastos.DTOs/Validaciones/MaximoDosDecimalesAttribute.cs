using System.ComponentModel.DataAnnotations;

namespace GestorGastos.DTOs.Validaciones
{
    public class MaximoDosDecimalesAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            if (value is not decimal monto)
                return true;

            return decimal.Round(monto, 2) == monto;
        }
    }
}
