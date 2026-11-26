using Makoto.Api.Contracts;
using Makoto.Api.Data;
using Makoto.Api.Models;
using Makoto.Api.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Makoto.Api.Controllers;

/// <summary>Endpoints del Dashboard. Protegidos con la clave temporal hasta el login JWT (T2-20).</summary>
[ApiController]
[Route("api/admin")]
[Produces("application/json")]
[ClaveAdminTemporal]
// T2-18: máximo de solicitudes por minuto y por IP (Program.cs, política "admin") contra la fuerza bruta
[EnableRateLimiting("admin")]
public class AdminController(MakotoDbContext db) : ControllerBase
{
    /// <summary>Catálogo de las 6 condiciones 2×3 y si su audio ya está certificado.</summary>
    [HttpGet("condiciones")]
    [ProducesResponseType<IReadOnlyList<CondicionResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IReadOnlyList<CondicionResponse>> Condiciones(CancellationToken ct) =>
        await db.Condiciones.AsNoTracking()
            .OrderBy(c => c.Id)
            .Select(c => new CondicionResponse(c.Id, c.Codigo, c.Genero, c.Nivel,
                                               c.F0ObjetivoHz, c.F0MedidoHz, c.Sha256 != null))
            .ToListAsync(ct);

    /// <summary>Sesiones completadas y en curso por celda: muestra si el balanceo funciona.</summary>
    [HttpGet("cuotas")]
    [ProducesResponseType<IReadOnlyList<CuotaResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    // T2-07: con el diseño intra-sujeto cuenta las sesiones por la voz que escucharon PRIMERO
    // (CondicionVocalId = primera del orden). Con el cuadrado latino deben quedar iguales.
    public async Task<IReadOnlyList<CuotaResponse>> Cuotas(CancellationToken ct) =>
        await db.Condiciones.AsNoTracking()
            .OrderBy(c => c.Id)
            .Select(c => new CuotaResponse(c.Codigo, c.Genero, c.Nivel,
                c.Sesiones.Count(s => s.Estado == EstadoSesion.Completada),
                c.Sesiones.Count(s => s.Estado == EstadoSesion.Iniciada || s.Estado == EstadoSesion.EnAudio)))
            .ToListAsync(ct);
}
