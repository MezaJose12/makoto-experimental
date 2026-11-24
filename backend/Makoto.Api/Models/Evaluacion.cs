namespace Makoto.Api.Models;

/// <summary>Cuestionario enviado (1:1 con la sesión) y chequeos de manipulación.</summary>
public class Evaluacion
{
    public Guid Id { get; set; }
    public Guid SesionId { get; set; }
    public string GeneroPercibido { get; set; } = "";
    public string TonoPercibido { get; set; } = "";
    public short Naturalidad { get; set; }
    public DateTime EnviadaUtc { get; set; }

    public SesionExperimental Sesion { get; set; } = null!;
    public ICollection<RespuestaItem> Respuestas { get; set; } = new List<RespuestaItem>();
}
