using GestorGastos.Entidades;
using GestorGastos.Repositorios;
using GestorGastos.Repositorios.Interfaces;
using GestorGastos.Servicios;
using GestorGastos.Servicios.Interfaces;
using GestorGastos.Web.Helpers;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<GestorGastosDbContext>(options =>
    options.UseSqlServer(ConexionBaseDatos.Obtener(builder.Configuration.GetConnectionString("GestorGastos")),
        sqlServer => sqlServer.EnableRetryOnFailure()));

builder.Services.AddScoped<IUsuarioRepositorio, UsuarioRepositorio>();
builder.Services.AddScoped<IUsuarioServicio, UsuarioServicio>();

builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(
        options =>
        {
            builder.Configuration.Bind("AzureAd", options);
            options.Events.OnTokenValidated = async contexto =>
            {
                var usuario = contexto.Principal!;
                var usuarioServicio = contexto.HttpContext.RequestServices.GetRequiredService<IUsuarioServicio>();
                await usuarioServicio.RegistrarAccesoAsync(usuario.GetObjectId()!, usuario.Nombre(), usuario.Email());
            };
        },
        cookies => cookies.AccessDeniedPath = "/Cuenta/AccesoDenegado");

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .RequireRole(Roles.Empleado, Roles.Administrador)
        .Build();
});

builder.Services.AddControllersWithViews();
builder.Services.AddProblemDetails();

if (!string.IsNullOrEmpty(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
    builder.Services.AddApplicationInsightsTelemetry();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<GestorGastosDbContext>();
    await db.Database.MigrateAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets().AllowAnonymous();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
