using Makoto.Api.Data;
using Makoto.Api.Models;
using static Makoto.Api.Data.CatalogoCondiciones;

namespace Makoto.Tests;

/// <summary>Protege el diseño 2×3: si alguien cambia un F0 a un valor ambiguo, la CI falla.</summary>
public class CatalogoCondicionesTests
{
    [Fact]
    public void HaySeisCeldas_ConCodigosYArchivosUnicos()
    {
        Assert.Equal(6, Celdas.Count);
        Assert.Equal(6, Celdas.Select(c => c.Codigo).Distinct().Count());
        Assert.Equal(6, Celdas.Select(c => c.Archivo).Distinct().Count());
        Assert.Equal(6, Celdas.Select(c => (c.Genero, c.Nivel)).Distinct().Count());
    }

    [Fact]
    public void NingunF0_TocaLaBandaAmbigua()
    {
        foreach (var c in Celdas)
            Assert.False(c.F0ObjetivoHz + ToleranciaHz >= BandaAmbiguaMinHz && c.F0ObjetivoHz - ToleranciaHz <= BandaAmbiguaMaxHz,
                $"{c.Codigo} ({c.F0ObjetivoHz} Hz) toca la banda {BandaAmbiguaMinHz}–{BandaAmbiguaMaxHz} Hz");
    }

    [Theory]
    [InlineData(GeneroVocal.Femenino)]
    [InlineData(GeneroVocal.Masculino)]
    public void NivelesDeCadaGenero_SubenAlMenosDosSemitonos(GeneroVocal genero)
    {
        var f0 = Celdas.Where(c => c.Genero == genero).OrderBy(c => c.Nivel).Select(c => (double)c.F0ObjetivoHz).ToArray();

        for (var i = 1; i < f0.Length; i++)
            Assert.True(12 * Math.Log2(f0[i] / f0[i - 1]) >= 2, $"{genero}: paso {f0[i - 1]} → {f0[i]} Hz menor a 2 semitonos");
    }

    [Theory]
    [InlineData("CondicionVocalId", "condicion_vocal_id")]
    [InlineData("F0ObjetivoHz", "f0_objetivo_hz")]
    [InlineData("Sha256", "sha256")]
    [InlineData("Id", "id")]
    public void NombresDeColumna_EnSnakeCase(string propiedad, string columna) =>
        Assert.Equal(columna, MakotoDbContext.ASnakeCase(propiedad));
}
