using System.ComponentModel.DataAnnotations;
using Makoto.Api.Models;

namespace Makoto.Api.Contracts;

// ── Lo que la PWA del estudiante envía y recibe. Nunca incluye género, nivel ni nombre del audio. ──

/// <summary>POST /api/sesiones · el estudiante acepta el consentimiento.</summary>
public sealed record IniciarSesionRequest(
    [Required] bool? ConsentimientoAceptado,
    [Required, StringLength(20, MinimumLength = 1)] string ConsentimientoVersion,
    [StringLength(40)] string? CodigoAcceso);

// T2-07: el participante escucha todas las voces (D-02). Cada pista se identifica solo por su posición
// («Voz 1», «Voz 2»…): la respuesta nunca revela género, frecuencia ni nombre del archivo.
public sealed record PistaResponse(int Indice, string Etiqueta, string AudioUrl);

public sealed record IniciarSesionResponse(
    Guid SesionId, EstadoSesion Estado, IReadOnlyList<PistaResponse> Pistas, int PistasEscuchadas);

/// <summary>GET /api/sesiones/{id} · para retomar si el estudiante recarga la página.</summary>
public sealed record EstadoSesionResponse(
    Guid SesionId, EstadoSesion Estado, IReadOnlyList<PistaResponse> Pistas, int PistasEscuchadas, bool Evaluada);

/// <summary>POST /api/sesiones/{id}/participante · covariables.</summary>
public sealed record DatosParticipanteRequest(
    [Required, StringLength(10)] string RangoEdad,
    [Required, StringLength(30)] string Genero,
    [Required, StringLength(30)] string ExperienciaAsistentesVoz);

/// <summary>POST /api/sesiones/{id}/eventos · telemetría del reproductor.</summary>
public sealed record EventoRequest(
    [Required] TipoEvento? Tipo,
    [Range(0, 600_000)] int? LatenciaMs);

/// <summary>POST /api/sesiones/{id}/evaluacion · cuestionario completo.</summary>
public sealed record EvaluacionRequest(
    [Required, StringLength(20)] string GeneroPercibido,
    [Required, StringLength(20)] string TonoPercibido,
    [Range(1, 7)] short Naturalidad,
    [Required, MinLength(1)] IReadOnlyList<RespuestaRequest> Respuestas);

public sealed record RespuestaRequest(int ItemId, short Valor);
