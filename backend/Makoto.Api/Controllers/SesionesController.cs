using Makoto.Api.Contracts;
using Makoto.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Makoto.Api.Controllers;

/// <summary>
/// Endpoints públicos que usa la PWA del estudiante.
/// T2-02 fija el contrato; la lógica llega en T2-05 (Asignador), T2-08 (Test) y T2-10 (Telemetría).
/// </summary>
[ApiController]
[Route("api/sesiones")]
[Produces("application/json")]
public class SesionesController(MakotoDbContext db) : ControllerBase
{
    /// <summary>Crea la sesión y asigna una de las 6 condiciones de forma balanceada.</summary>
    [HttpPost]
    [ProducesResponseType<IniciarSesionResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult Iniciar(IniciarSesionRequest request) => Pendiente("T2-05 · Asignador de condiciones");

    /// <summary>Estado de la sesión (para retomar si el estudiante recarga la página).</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<EstadoSesionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EstadoSesionResponse>> Obtener(Guid id, CancellationToken ct)
    {
        var sesion = await db.Sesiones.AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new EstadoSesionResponse(s.Id, s.Estado, s.Evaluacion != null))
            .SingleOrDefaultAsync(ct);

        return sesion is null ? NotFound() : sesion;
    }

    /// <summary>Entrega el audio de la condición asignada a esta sesión (y solo ese).</summary>
    [HttpGet("{id:guid}/audio")]
    [Produces("audio/mpeg")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Audio(Guid id) => Pendiente("T2-05 · Asignador de condiciones");

    /// <summary>Guarda las covariables del participante.</summary>
    [HttpPost("{id:guid}/participante")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult Participante(Guid id, DatosParticipanteRequest request) => Pendiente("T2-08 · Back-end del Test");

    /// <summary>Registra un evento del reproductor (inicio, fin, repetición).</summary>
    [HttpPost("{id:guid}/eventos")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult Eventos(Guid id, EventoRequest request) => Pendiente("T2-10 · Telemetría");

    /// <summary>Guarda el cuestionario y cierra la sesión.</summary>
    [HttpPost("{id:guid}/evaluacion")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public IActionResult Evaluacion(Guid id, EvaluacionRequest request) => Pendiente("T2-08 · Back-end del Test");

    private ObjectResult Pendiente(string tarea) => Problem(
        title: "Pendiente de implementar",
        detail: $"Este endpoint ya forma parte del contrato y se implementa en {tarea}.",
        statusCode: StatusCodes.Status501NotImplemented);
}
