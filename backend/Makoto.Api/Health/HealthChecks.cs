using Makoto.Api.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Makoto.Api.Health;

/// <summary>Comprueba que la API puede abrir una conexión con PostgreSQL.</summary>
public sealed class DatabaseHealthCheck(MakotoDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            return await db.Database.CanConnectAsync(ct)
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("Sin conexión con PostgreSQL.");
        }
        catch (Exception ex)
        {
            // El detalle va al log del servidor, nunca a la respuesta HTTP.
            return HealthCheckResult.Unhealthy("Sin conexión con PostgreSQL.", ex);
        }
    }
}

/// <summary>Comprueba que la carpeta de audios existe y se puede leer.</summary>
public sealed class AudiosHealthCheck(AudiosOptions options) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        var ok = Directory.Exists(options.RutaAbsoluta);
        return Task.FromResult(ok
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("No se encuentra la carpeta de audios."));
    }
}

/// <summary>Ruta de la carpeta de audios, resuelta al arrancar.</summary>
public sealed record AudiosOptions(string RutaAbsoluta);
