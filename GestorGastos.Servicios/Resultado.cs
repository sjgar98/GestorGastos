namespace GestorGastos.Servicios
{
    public class Resultado<T>
    {
        private Resultado(T? valor, TipoError? error, string? mensaje)
        {
            Valor = valor;
            Error = error;
            Mensaje = mensaje;
        }

        public bool Exitoso => Error == null;
        public T? Valor { get; }
        public TipoError? Error { get; }
        public string? Mensaje { get; }

        public static Resultado<T> Exito(T valor)
        {
            return new Resultado<T>(valor, null, null);
        }

        public static Resultado<T> Fallo(TipoError error, string mensaje)
        {
            return new Resultado<T>(default, error, mensaje);
        }
    }
}