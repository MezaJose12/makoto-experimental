namespace Makoto.Api.Models;

/// <summary>Una de las 6 celdas del diseño 2×3 (género × nivel de F0) y su audio certificado.</summary>
public class CondicionVocal
{
    public int Id { get; set; }
    public string Codigo { get; set; } = "";              // F_GRAVE … M_AGUDA
    public GeneroVocal Genero { get; set; }
    public NivelFrecuencia Nivel { get; set; }
    public decimal F0ObjetivoHz { get; set; }
    public decimal? F0MedidoHz { get; set; }              // lo mide Praat (T2-04)
    public decimal? DuracionSegundos { get; set; }
    public string Archivo { get; set; } = "";             // nombre interno: nunca se envía al participante
    public string? Sha256 { get; set; }                   // huella del audio certificado
    public string? VozTts { get; set; }                   // voz, versión y fecha de síntesis

    public ICollection<SesionExperimental> Sesiones { get; set; } = new List<SesionExperimental>();
}
