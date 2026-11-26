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
/// T2-07: diseño intra-sujeto (D-02). Cada sesión tiene un orden de voces; el audio se pide por pista
/// (/pistas/{indice}/audio) y solo se desbloquea la siguiente cuando la anterior se escuchó completa
/// (/pistas/{indice}/fin).
/// </summary>
[ApiController]
[Route("api/sesiones")]
[Produces("application/json")]
// T2-05: se inyectan el Asignador y la ruta de los audios para Iniciar y Audio
public class SesionesController(MakotoDbContext db, AsignadorCondiciones asignador, AudiosOptions audios) : ControllerBase
{
    // T2-07: pistas de una sesión, identificadas solo por su posición
    private static List<PistaResponse> Pistas(Guid id, int total) =>
        Enumerable.Range(1, total)
            .Select(i => new PistaResponse(i, $"Voz {i}", $"/api/sesiones/{id}/pistas/{i}/audio"))
            .ToList();

    /// <summary>Crea la sesión y asigna un orden contrabalanceado de todas las voces certificadas.</summary>
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
            // T2-07: la respuesta no revela las condiciones; cada audio se pide por su posición
            var respuesta = new IniciarSesionResponse(sesion.Id, sesion.Estado,
                Pistas(sesion.Id, sesion.OrdenCondiciones.Length), sesion.PistasEscuchadas);
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
        // T2-07: devuelve también las pistas y cuántas escuchó, para continuar donde quedó
        var sesion = await db.Sesiones.AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new { s.Id, s.Estado, Total = s.OrdenCondiciones.Length, s.PistasEscuchadas, Evaluada = s.Evaluacion != null })
            .SingleOrDefaultAsync(ct);

        return sesion is null
            ? NotFound()
            : new EstadoSesionResponse(sesion.Id, sesion.Estado, Pistas(sesion.Id, sesion.Total), sesion.PistasEscuchadas, sesion.Evaluada);
    }

    /// <summary>Entrega el audio de la pista <paramref name="indice"/> (1, 2, …) de esta sesión.</summary>
    [HttpGet("{id:guid}/pistas/{indice:int}/audio")]
    [Produces("audio/mpeg")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Audio(Guid id, int indice, CancellationToken ct)
    {
        // T2-07: reemplaza a /{id}/audio de T2-05. El servidor busca la voz de ESA posición en el orden
        // de ESTA sesión; el participante nunca ve qué condición es.
        var sesion = await db.Sesiones.AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new { s.Estado, s.OrdenCondiciones, s.PistasEscuchadas })
            .SingleOrDefaultAsync(ct);

        if (sesion is null || indice < 1 || indice > sesion.OrdenCondiciones.Length)
            return NotFound();
        if (sesion.Estado is EstadoSesion.Completada or EstadoSesion.Expirada)
            return Problem(title: "Sesión cerrada", detail: "Esta sesión ya terminó o expiró.", statusCode: StatusCodes.Status409Conflict);
        // T2-07: reproducción en orden estricto: no se puede saltar a una voz posterior
        if (indice > sesion.PistasEscuchadas + 1)
            return Problem(title: "Pista bloqueada", detail: "Termine de escuchar la voz anterior.", statusCode: StatusCodes.Status409Conflict);

        var condicionId = sesion.OrdenCondiciones[indice - 1];
        var condicion = await db.Condiciones.AsNoTracking()
            .Where(c => c.Id == condicionId)
            .Select(c => new { c.Archivo, Certificada = c.Sha256 != null })
            .SingleAsync(ct);

        // T2-05: solo se entrega un audio certificado e intacto (la huella se verificó al arrancar, T2-04)
        var ruta = Path.Combine(audios.RutaAbsoluta, CertificacionAudios.Carpeta, Path.GetFileName(condicion.Archivo));
        if (!condicion.Certificada || !System.IO.File.Exists(ruta))
            return Problem(title: "Audio no disponible", detail: "El audio de esta voz no está certificado.",
                statusCode: StatusCodes.Status503ServiceUnavailable);

        // T2-05: sin caché en el navegador ni en la PWA; con rangos para que el reproductor pueda bufferizar
        Response.Headers.CacheControl = "no-store";
        return PhysicalFile(ruta, "audio/mpeg", enableRangeProcessing: true);
    }

    /// <summary>El reproductor avisa que la pista <paramref name="indice"/> terminó: se desbloquea la siguiente.</summary>
    [HttpPost("{id:guid}/pistas/{indice:int}/fin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> FinPista(Guid id, int indice, CancellationToken ct)
    {
        // T2-07: la duración real de escucha se valida con la telemetría (T2-10)
        var sesion = await db.Sesiones.SingleOrDefaultAsync(s => s.Id == id, ct);
        if (sesion is null || indice < 1 || indice > sesion.OrdenCondiciones.Length)
            return NotFound();
        if (sesion.Estado is EstadoSesion.Completada or EstadoSesion.Expirada)
            return Problem(title: "Sesión cerrada", detail: "Esta sesión ya terminó o expiró.", statusCode: StatusCodes.Status409Conflict);
        if (indice > sesion.PistasEscuchadas + 1)
            return Problem(title: "Pista bloqueada", detail: "Termine de escuchar la voz anterior.", statusCode: StatusCodes.Status409Conflict);

        // Repetir el aviso de una pista ya terminada no cambia nada (el navegador puede reintentar)
        if (indice == sesion.PistasEscuchadas + 1)
        {
            sesion.PistasEscuchadas = indice;
            sesion.Estado = EstadoSesion.EnAudio;
            // T2-07: la sesión dura ~25 min; cada pista terminada renueva la reserva para que no expire a mitad
            var renovada = DateTime.UtcNow.AddMinutes(asignador.ReservaMinutos);
            if (renovada > sesion.ReservadaHastaUtc)
                sesion.ReservadaHastaUtc = renovada;
            await db.SaveChangesAsync(ct);
        }
        return NoContent();
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
