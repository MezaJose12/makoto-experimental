namespace Makoto.Api.Models;

/// <summary>Una participación anónima. No guarda nombre, correo ni IP.</summary>
public class SesionExperimental
{
    public Guid Id { get; set; }
    public string CodigoParticipante { get; set; } = "";  // lo genera el servidor
    public int CondicionVocalId { get; set; }
    public EstadoSesion Estado { get; set; } = EstadoSesion.Iniciada;
    public string ConsentimientoVersion { get; set; } = "";
    public DateTime ConsentimientoUtc { get; set; }
    public DateTime ReservadaHastaUtc { get; set; }       // inicio + 30 min: libera celdas abandonadas
    public DateTime InicioUtc { get; set; }
    public DateTime? FinUtc { get; set; }

    public CondicionVocal CondicionVocal { get; set; } = null!;
    public DatosParticipante? DatosParticipante { get; set; }
    public Evaluacion? Evaluacion { get; set; }
    public ICollection<EventoTelemetria> Eventos { get; set; } = new List<EventoTelemetria>();
}
