namespace Makoto.Api.Services;

/// <summary>
/// T2-07 (D-02 intra-sujeto): órdenes de presentación contrabalanceados (cuadrado latino de Williams).
/// Con n voces:
///  · cada voz aparece una vez en cada posición (1.ª, 2.ª, …) a lo largo de las filas;
///  · cada par «voz A seguida de voz B» aparece el mismo número de veces (controla el efecto de arrastre).
/// Con n par basta con n filas; con n impar se agregan las n filas invertidas (2n filas).
/// Cada participante recibe una fila; el Asignador reparte las filas por igual.
/// </summary>
public static class CuadradoLatino
{
    /// <summary>Filas del diseño. Cada fila es una permutación de los índices 0..n-1.</summary>
    public static IReadOnlyList<int[]> Williams(int n)
    {
        if (n < 1)
            throw new ArgumentOutOfRangeException(nameof(n), "Se necesita al menos una voz.");

        // Primera fila: 0, 1, n-1, 2, n-2, 3, …
        var primera = new int[n];
        int bajo = 1, alto = n - 1;
        for (var j = 1; j < n; j++)
            primera[j] = j % 2 == 1 ? bajo++ : alto--;

        // Las demás filas suman r (módulo n) a la primera
        var filas = Enumerable.Range(0, n)
            .Select(r => primera.Select(x => (x + r) % n).ToArray())
            .ToList();

        if (n % 2 == 1)
            filas.AddRange(filas.Select(f => f.Reverse().ToArray()).ToList());

        return filas;
    }
}
