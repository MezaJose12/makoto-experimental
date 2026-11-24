namespace Makoto.Api.Models;

/// <summary>Covariables del participante (1:1 con la sesión). Rango de edad, nunca la edad exacta.</summary>
public class DatosParticipante
{
    public Guid SesionId { get; set; }
    public string RangoEdad { get; set; } = "";           // 18-20, 21-23, 24-26, 27+
    public string Genero { get; set; } = "";
    public string ExperienciaAsistentesVoz { get; set; } = "";

    public SesionExperimental Sesion { get; set; } = null!;
}
