using System.Text.RegularExpressions;
using Makoto.Api.Services;

namespace Makoto.Tests;

/// <summary>T2-05: reglas del Asignador que no necesitan base de datos.</summary>
public class AsignadorCondicionesTests
{
    [Fact]
    public void Elige_LaCeldaMenosOcupada()
    {
        var ocupacion = new Dictionary<int, int> { [1] = 4, [2] = 3, [3] = 4, [4] = 5, [5] = 4, [6] = 4 };

        Assert.Equal(2, AsignadorCondiciones.ElegirCondicion(ocupacion, new Random(1)));
    }

    [Fact]
    public void EnEmpate_TodasLasCeldasEmpatadasPuedenSalir()
    {
        var ocupacion = new Dictionary<int, int> { [1] = 2, [2] = 2, [3] = 9, [4] = 2, [5] = 9, [6] = 2 };
        var azar = new Random(7);

        var elegidas = Enumerable.Range(0, 400).Select(_ => AsignadorCondiciones.ElegirCondicion(ocupacion, azar)).ToHashSet();

        Assert.Equal(new HashSet<int> { 1, 2, 4, 6 }, elegidas);
    }

    [Fact]
    public void Asignaciones_Sucesivas_QuedanPerfectamenteBalanceadas()
    {
        var ocupacion = Enumerable.Range(1, 6).ToDictionary(i => i, _ => 0);
        var azar = new Random(42);

        for (var i = 0; i < 600; i++)
            ocupacion[AsignadorCondiciones.ElegirCondicion(ocupacion, azar)]++;

        Assert.All(ocupacion.Values, v => Assert.Equal(100, v));
    }

    [Fact]
    public void SinCondiciones_LanzaError() =>
        Assert.Throws<ArgumentException>(() => AsignadorCondiciones.ElegirCondicion(new Dictionary<int, int>(), new Random()));

    [Fact]
    public void CodigoDeParticipante_EsAnonimo_Legible_YNoSeRepite()
    {
        var codigos = Enumerable.Range(0, 5000).Select(_ => AsignadorCondiciones.GenerarCodigoParticipante()).ToList();

        Assert.All(codigos, c => Assert.Matches(new Regex("^MK-[A-HJ-NP-Z2-9]{8}$"), c));
        Assert.Equal(codigos.Count, codigos.Distinct().Count());
        Assert.All(codigos, c => Assert.True(c.Length <= 12));   // cabe en la columna codigo_participante
    }
}
