using System.Text.Json;
using System.Text.Json.Serialization;
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
    app.UseExceptionHandler();

app.UseCors("FrontendPwa");
app.UseAuthorization();
app.MapControllers();

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
