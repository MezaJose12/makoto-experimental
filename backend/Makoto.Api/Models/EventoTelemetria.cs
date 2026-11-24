namespace Makoto.Api.Models;

/// <summary>Prueba de que el participante escuchó el audio (inicio, fin, repeticiones).</summary>
public class EventoTelemetria
{
    public long Id { get; set; }
    public Guid SesionId { get; set; }
    public TipoEvento Tipo { get; set; }
    public DateTime OcurridoUtc { get; set; }
    public int? LatenciaMs { get; set; }
    public string? Metadatos { get; set; }                // JSON (jsonb)

    public SesionExperimental Sesion { get; set; } = null!;
}
