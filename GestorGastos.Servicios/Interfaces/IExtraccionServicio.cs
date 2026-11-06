namespace GestorGastos.Servicios.Interfaces
{
    public interface IExtraccionServicio
    {
        Task ProcesarAsync(string blobNombre, Stream contenido, CancellationToken cancelacion);
    }
}
