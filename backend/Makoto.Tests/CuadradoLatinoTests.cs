using Makoto.Api.Services;

namespace Makoto.Tests;

/// <summary>T2-07: el orden de las voces está contrabalanceado (cuadrado latino de Williams).</summary>
public class CuadradoLatinoTests
{
    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public void CadaFila_EsUnaPermutacionDeTodasLasVoces(int n)
    {
        foreach (var fila in CuadradoLatino.Williams(n))
            Assert.Equal(Enumerable.Range(0, n), fila.Order());
    }

    [Theory]
    [InlineData(5)]
    [InlineData(6)]
    public void CadaVoz_ApareceIgualEnCadaPosicion(int n)
    {
        var filas = CuadradoLatino.Williams(n);
        for (var posicion = 0; posicion < n; posicion++)
        {
            var conteo = filas.GroupBy(f => f[posicion]).Select(g => g.Count()).ToList();
            Assert.Equal(n, conteo.Count);
            Assert.All(conteo, c => Assert.Equal(filas.Count / n, c));
        }
    }

    [Theory]
    [InlineData(5)]
    [InlineData(6)]
    public void CadaPar_VozASeguidaDeVozB_ApareceIgual(int n)
    {
        var pares = CuadradoLatino.Williams(n)
            .SelectMany(f => f.Zip(f.Skip(1)))
            .GroupBy(p => p)
            .Select(g => g.Count())
            .ToList();

        Assert.Equal(n * (n - 1), pares.Count);           // aparecen todos los pares posibles
        Assert.Single(pares.Distinct());                   // y todos el mismo número de veces
    }

    [Fact]
    public void ConSeisVoces_HaySeisFilas_YCadaUnaEmpiezaPorUnaVozDistinta()
    {
        var filas = CuadradoLatino.Williams(6);

        Assert.Equal(6, filas.Count);
        Assert.Equal(Enumerable.Range(0, 6), filas.Select(f => f[0]).Order());
    }
}
