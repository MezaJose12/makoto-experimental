namespace Makoto.Api.Models;

/// <summary>Valor que un participante dio a un ítem (clave compuesta evaluación + ítem).</summary>
public class RespuestaItem
{
    public Guid EvaluacionId { get; set; }
    public int ItemId { get; set; }
    public short Valor { get; set; }

    public Evaluacion Evaluacion { get; set; } = null!;
    public InstrumentoItem Item { get; set; } = null!;
}
