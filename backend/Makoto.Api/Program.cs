using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Makoto.Api.Data;
using Makoto.Api.Health;
using Makoto.Api.Services;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ── Puerto ───────────────────────────────────────────────────────────────
// Docker usa 8080 (ASPNETCORE_HTTP_PORTS). Si la plataforma define PORT (Render), se respeta.
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// ── Base de datos ────────────────────────────────────────────────────────
// DATABASE_URL (Neon) tiene prioridad; si no existe, ConnectionStrings:Default (Docker Compose).
var connectionString = ConnectionStringResolver.Resolve(
    Environment.GetEnvironmentVariable("DATABASE_URL"),
    builder.Configuration.GetConnectionString("Default"));

builder.Services.AddDbContext<MakotoDbContext>(options =>
    options.UseNpgsql(connectionString, npgsql =>
        npgsql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorCodesToAdd: null)));

// ── Audios ───────────────────────────────────────────────────────────────
var rutaAudios = builder.Configuration["Audios:Ruta"] ?? "datos/audios";
if (!Path.IsPathRooted(rutaAudios))
    rutaAudios = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, rutaAudios));
builder.Services.AddSingleton(new AudiosOptions(rutaAudios));

// T2-05: Asignador de condiciones (balanceo 2×3 con candado de PostgreSQL), uno por solicitud
builder.Services.AddScoped<AsignadorCondiciones>();

// ── CORS: solo los orígenes configurados (la PWA) ────────────────────────
var origenes = builder.Configuration.GetSection("Cors:Origenes").Get<string[]>() ?? [];
builder.Services.AddCors(o => o.AddPolicy("FrontendPwa", p =>
    p.WithOrigins(origenes).WithMethods("GET", "POST").AllowAnyHeader()));

// ── Salud ────────────────────────────────────────────────────────────────
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("postgresql", tags: ["ready"], timeout: TimeSpan.FromSeconds(15))
    .AddCheck<AudiosHealthCheck>("audios", tags: ["ready"]);

// T2-18: en Render la clave del panel queda expuesta a internet. Se limitan las solicitudes al panel
// por IP (por defecto 30 por minuto) para que nadie pueda probar claves por fuerza bruta.
// Solo el panel: los estudiantes de un laboratorio comparten IP y no deben bloquearse entre sí.
var adminPorMinuto = builder.Configuration.GetValue("Seguridad:AdminSolicitudesPorMinuto", 30);
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy("admin", http => RateLimitPartition.GetFixedWindowLimiter(
        http.Connection.RemoteIpAddress?.ToString() ?? "sin-ip",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = adminPorMinuto,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));
});

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();   // errores en formato estándar, sin detalles internos

var app = builder.Build();

// ── Migraciones y certificación de audios (local, Docker y Neon) ──
if (app.Configuration.GetValue("Database:AplicarMigraciones", true))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<MakotoDbContext>();
    await db.Database.MigrateAsync();
    await SincronizadorCertificacion.SincronizarAsync(db, rutaAudios, app.Logger);
}

if (app.Environment.IsDevelopment())
    app.MapOpenApi();                    // /openapi/v1.json solo en desarrollo
else
{
    app.UseExceptionHandler();
    // T2-18: el navegador recordará usar solo HTTPS con este dominio (Render ya redirige HTTP → HTTPS)
    app.UseHsts();

    // T2-18: avisos en el log de Render si la configuración deja el estudio abierto
    if (string.IsNullOrEmpty(app.Configuration["Estudio:CodigoAcceso"]))
        app.Logger.LogWarning("Estudio:CodigoAcceso está vacío: cualquiera que tenga la URL puede crear sesiones y llenar las cuotas.");
    if ((app.Configuration["Admin:ClaveTemporal"]?.Length ?? 0) < 24)
        app.Logger.LogWarning("Admin:ClaveTemporal falta o tiene menos de 24 caracteres: use una clave larga y aleatoria.");
}

// T2-18: cabeceras de seguridad en todas las respuestas (API y PWA).
// CSP: solo se cargan scripts, audios y llamadas del propio dominio; la página no se puede incrustar.
app.Use(async (context, next) =>
{
    var h = context.Response.Headers;
    h["X-Content-Type-Options"] = "nosniff";
    h["X-Frame-Options"] = "DENY";
    h["Referrer-Policy"] = "no-referrer";
    h["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    h["Content-Security-Policy"] =
        "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; " +
        "media-src 'self'; connect-src 'self'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'";
    await next();
});

// T2-18: la PWA compilada viaja en wwwroot (Dockerfile.render) y se sirve desde el mismo origen.
// index.html y el service worker no se guardan en caché (así cada despliegue llega de inmediato);
// los archivos de /assets llevan huella en el nombre y pueden guardarse un año.
var opcionesPwa = new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        var nombre = ctx.File.Name;
        if (nombre is "index.html" or "sw.js" or "registerSW.js" or "manifest.webmanifest")
            ctx.Context.Response.Headers.CacheControl = "no-cache";
        else if (ctx.Context.Request.Path.StartsWithSegments("/assets"))
            ctx.Context.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
    }
};
app.UseDefaultFiles();
app.UseStaticFiles(opcionesPwa);

app.UseCors("FrontendPwa");
app.UseRateLimiter();   // T2-18: aplica la política "admin" al AdminController
app.UseAuthorization();
app.MapControllers();

// T2-18: rutas de la PWA (/test, /admin…) devuelven index.html para que React Router las resuelva.
// Una ruta /api inexistente sigue respondiendo 404 (no la página de la PWA).
// Solo si la imagen trae la PWA: en Docker Compose y en las pruebas no hay wwwroot.
if (File.Exists(Path.Combine(app.Environment.WebRootPath ?? "", "index.html")))
{
    app.MapFallback("/api/{**resto}", () => Results.NotFound());
    app.MapFallbackToFile("index.html", opcionesPwa);
}

// /health: el proceso está vivo. No toca la base de datos (Render lo usa como health check
// y así Neon puede apagarse por inactividad).
app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => false });

// /health/ready: diagnóstico de PostgreSQL y audios. Solo estados, nunca mensajes internos.
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            estado = report.Status.ToString(),
            componentes = report.Entries.ToDictionary(e => e.Key, e => e.Value.Status.ToString())
        }));
    }
});

app.Run();

// Permite que las pruebas de integración creen la aplicación (WebApplicationFactory).
public partial class Program;
