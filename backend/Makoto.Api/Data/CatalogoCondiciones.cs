using Makoto.Api.Models;

namespace Makoto.Api.Data;

/// <summary>
/// Las 6 celdas del diseño 2×3 que se siembran en la base de datos.
/// Niveles definidos dentro de cada género y fuera de la banda ambigua 145–165 Hz (±5 Hz de tolerancia).
/// El valor que cuenta para el análisis es el F0 medido con Praat (F0MedidoHz), no este objetivo.
/// </summary>
public static class CatalogoCondiciones
{
    public const decimal BandaAmbiguaMinHz = 145m;
    public const decimal BandaAmbiguaMaxHz = 165m;
    public const decimal ToleranciaHz = 5m;

    public sealed record Celda(int Id, string Codigo, GeneroVocal Genero, NivelFrecuencia Nivel, decimal F0ObjetivoHz, string Archivo);

    public static readonly IReadOnlyList<Celda> Celdas =
    [
        new(1, "F_GRAVE",  GeneroVocal.Femenino,  NivelFrecuencia.Grave, 175m, "f_grave.mp3"),
        new(2, "F_NEUTRA", GeneroVocal.Femenino,  NivelFrecuencia.Neutra, 210m, "f_neutra.mp3"),
        new(3, "F_AGUDA",  GeneroVocal.Femenino,  NivelFrecuencia.Aguda, 255m, "f_aguda.mp3"),
        new(4, "M_GRAVE",  GeneroVocal.Masculino, NivelFrecuencia.Grave, 100m, "m_grave.mp3"),
        new(5, "M_NEUTRA", GeneroVocal.Masculino, NivelFrecuencia.Neutra, 118m, "m_neutra.mp3"),
        new(6, "M_AGUDA",  GeneroVocal.Masculino, NivelFrecuencia.Aguda, 138m, "m_aguda.mp3"),
    ];
}
