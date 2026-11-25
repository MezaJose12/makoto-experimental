using Makoto.Api.Contracts;
using Makoto.Api.Data;
using Makoto.Api.Health;
using Makoto.Api.Models;
using Makoto.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Makoto.Api.Controllers;

/// <summary>
/// Endpoints públicos que usa la PWA del estudiante.
/// T2-02 fija el contrato; la lógica llega en T2-05 (Asignador), T2-08 (Test) y T2-10 (Telemetría).
/// T2-05: Iniciar y Audio ya están implementados.
/// </summary>
[ApiController]
[Route("api/sesiones")]
[Produces("application/json")]
// T2-05: se inyectan el Asignador y la ruta de los audios para Iniciar y Audio
public class SesionesController(MakotoDbContext db, AsignadorCondiciones asignador, AudiosOptions audios) : ControllerBase
{
    /// <summary>Crea la sesión y asigna una de las 6 condiciones de forma balanceada.</summary>
    [HttpPost]
    [ProducesResponseType<IniciarSesionResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<IniciarSesionResponse>> Iniciar(IniciarSesionRequest request, CancellationToken ct)
    {
        // T2-05: sin consentimiento explícito no se crea ningún registro
        if (request.ConsentimientoAceptado != true)
            return Problem(title: "Consentimiento requerido",
                detail: "Para participar debe aceptar el consentimiento informado.", statusCode: StatusCodes.Status400BadRequest);

        // T2-05: código del estudio (si está configurado) para que personas ajenas no llenen las cuotas
        if (!asignador.CodigoAccesoValido(request.CodigoAcceso))
            return Problem(title: "Código de acceso inválido",
                detail: "Solicite el código del estudio a la persona que coordina la sesión.", statusCode: StatusCodes.Status403Forbidden);

        try
        {
            var sesion = await asignador.IniciarAsync(request.ConsentimientoVersion, ct);
            // T2-05: la respuesta no revela la condición; el audio se pide por el id de la sesión
            var respuesta = new IniciarSesionResponse(sesion.Id, sesion.Estado, $"/api/sesiones/{sesion.Id}/audio");
            return CreatedAtAction(nameof(Obtener), new { id = sesion.Id }, respuesta);
        }
        catch (EstimulosNoDisponiblesException ex)
        {
            return Problem(title: "Estímulos no disponibles", detail: ex.Message, statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }

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
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Audio(Guid id, CancellationToken ct)
    {
        // T2-05: el servidor busca la celda de ESTA sesión; el participante nunca ve qué condición le tocó
        var datos = await db.Sesiones.AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new { s.Estado, s.CondicionVocal.Archivo, Certificada = s.CondicionVocal.Sha256 != null })
            .SingleOrDefaultAsync(ct);

        if (datos is null)
            return NotFound();
        if (datos.Estado is EstadoSesion.Completada or EstadoSesion.Expirada)
            return Problem(title: "Sesión cerrada", detail: "Esta sesión ya terminó o expiró.", statusCode: StatusCodes.Status409Conflict);

        // T2-05: solo se entrega un audio certificado e intacto (la huella se verificó al arrancar, T2-04)
        var ruta = Path.Combine(audios.RutaAbsoluta, CertificacionAudios.Carpeta, Path.GetFileName(datos.Archivo));
        if (!datos.Certificada || !System.IO.File.Exists(ruta))
            return Problem(title: "Audio no disponible", detail: "El audio de esta condición no está certificado.",
                statusCode: StatusCodes.Status503ServiceUnavailable);

        // T2-05: sin caché en el navegador ni en la PWA; con rangos para que el reproductor pueda bufferizar
        Response.Headers.CacheControl = "no-store";
        return PhysicalFile(ruta, "audio/mpeg", enableRangeProcessing: true);
    }

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
