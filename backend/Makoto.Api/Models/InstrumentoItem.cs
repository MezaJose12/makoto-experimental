namespace Makoto.Api.Models;

/// <summary>Ítem de un cuestionario (PETS, calidez, ítem único). Los textos se cargan cuando la traducción esté validada.</summary>
public class InstrumentoItem
{
    public int Id { get; set; }
    public string Instrumento { get; set; } = "";         // PETS, CALIDEZ, EMPATIA_UNICA
    public string Codigo { get; set; } = "";              // p. ej. PETS_01
    public string TextoEs { get; set; } = "";
    public string? Factor { get; set; }
    public short EscalaMin { get; set; }
    public short EscalaMax { get; set; }
    public bool Invertido { get; set; }
    public int Orden { get; set; }
    public bool Activo { get; set; } = true;

    public ICollection<RespuestaItem> Respuestas { get; set; } = new List<RespuestaItem>();
}
