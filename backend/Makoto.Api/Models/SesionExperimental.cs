namespace Makoto.Api.Models;

/// <summary>Una participación anónima. No guarda nombre, correo ni IP.</summary>
public class SesionExperimental
{
    public Guid Id { get; set; }
    public string CodigoParticipante { get; set; } = "";  // lo genera el servidor
    // T2-07: con el diseño intra-sujeto (D-02) es la PRIMERA voz del orden. Se conserva porque
    // reparte por igual la voz que cada participante escucha primero (sin efecto de arrastre) y
    // mantiene las cuotas del Dashboard y la prueba de concurrencia.
    public int CondicionVocalId { get; set; }

    // T2-07: ids de las condiciones en el orden en que este participante las escucha (cuadrado latino)
    public int[] OrdenCondiciones { get; set; } = [];

    // T2-07: fila del cuadrado latino asignada (el Asignador reparte las filas por igual)
    public int FilaOrden { get; set; }

    // T2-07: cuántas pistas escuchó completas. Solo puede reproducir la siguiente (no se salta ninguna).
    public int PistasEscuchadas { get; set; }
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
