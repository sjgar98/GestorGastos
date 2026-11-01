using GestorGastos.Entidades;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.Services.AddDbContext<GestorGastosDbContext>(options =>
    options.UseSqlServer(ConexionBaseDatos.Obtener(builder.Configuration.GetConnectionString("GestorGastos"))));

if (!string.IsNullOrEmpty(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
{
    builder.Services
        .AddApplicationInsightsTelemetryWorkerService()
        .ConfigureFunctionsApplicationInsights();
}

// TODO Extracción: ExtraerComprobanteFunction (Blob Trigger sobre "comprobantes") y registro de los servicios compartidos.

builder.Build().Run();
