using GestorGastos.Entidades;
using GestorGastos.Repositorios;
using GestorGastos.Repositorios.Interfaces;
using GestorGastos.Servicios;
using GestorGastos.Servicios.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = FunctionsApplication.CreateBuilder(args);

builder.Services.AddDbContext<GestorGastosDbContext>(options =>
    options.UseSqlServer(ConexionBaseDatos.Obtener(builder.Configuration.GetConnectionString("GestorGastos")),
        sqlServer => sqlServer.EnableRetryOnFailure()));

if (!string.IsNullOrEmpty(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
{
    builder.Services
        .AddApplicationInsightsTelemetryWorkerService()
        .ConfigureFunctionsApplicationInsights();

    // El SDK 2.x de Application Insights agrega una regla que solo envía logs Warning o mayores;
    // sin quitarla no llegan los logs Information de extracción y cambios de estado.
    builder.Logging.Services.Configure<LoggerFilterOptions>(opciones =>
    {
        var reglaSoloWarning = opciones.Rules.FirstOrDefault(regla =>
            regla.ProviderName == "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider");
        if (reglaSoloWarning is not null)
            opciones.Rules.Remove(reglaSoloWarning);
    });
}

builder.Services.AddScoped<IComprobanteRepositorio, ComprobanteRepositorio>();
builder.Services.AddScoped<IComprobanteServicio, ComprobanteServicio>();
builder.Services.AddScoped<IExtraccionServicio, ExtraccionServicio>();
builder.Services.AddSingleton<IAlmacenamientoArchivos>(_ => new AlmacenamientoArchivos(
    builder.Configuration["Storage:ConnectionString"] ?? string.Empty,
    builder.Configuration["Storage:Contenedor"] ?? string.Empty));

var endpointDocumentIntelligence = builder.Configuration["DocumentIntelligence:Endpoint"];
if (string.IsNullOrWhiteSpace(endpointDocumentIntelligence))
{
    builder.Services.AddSingleton<IExtractorDocumentos, ExtractorDocumentosMock>();
}
else
{
    builder.Services.AddSingleton<IExtractorDocumentos>(_ => new ExtractorDocumentIntelligence(
        endpointDocumentIntelligence,
        builder.Configuration["DocumentIntelligence:Key"] ?? string.Empty));
}

builder.Build().Run();
