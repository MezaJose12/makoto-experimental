using System.Security.Cryptography;
using System.Text;
using Makoto.Api.Models;
using Makoto.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Makoto.Api.Services;

/// <summary>Ninguna condición tiene su audio certificado: no se puede asignar a nadie.</summary>
public sealed class EstimulosNoDisponiblesException()
    : Exception("No hay condiciones con audio certificado (ejecute la certificación de T2-04).");

/// <summary>
/// T2-07: con el diseño intra-sujeto (D-02) cada participante escucha TODAS las voces certificadas.
/// El Asignador ya no elige una celda sino un ORDEN: la fila del cuadrado latino de Williams menos
/// usada (empates al azar). Así cada voz aparece por igual en cada posición. La transacción, el
/// candado y la expiración de reservas de T2-05 se mantienen.
///
/// T2-05 · Asignador de condiciones: decide qué voz escucha cada participante.
/// Asigna la celda 2×3 con menos ocupación (sesiones completadas + reservas vigentes) y desempata
/// al azar, de modo que las 6 celdas reciban la misma cantidad de participantes. Toda la operación
/// ocurre dentro de una transacción con un candado de PostgreSQL: si llegan varios participantes
/// a la vez se atienden de uno en uno y dos sesiones nunca toman la misma plaza.
/// </summary>
public class AsignadorCondiciones(MakotoDbContext db, IConfiguration configuracion)
{
    // Identificador arbitrario del candado de PostgreSQL (pg_advisory_xact_lock) usado solo por el Asignador
    private const long ClaveCandado = 7_231_001;

    // Sin 0/O ni 1/I/L: el código se lee y se dicta sin confusiones
    private const string AlfabetoCodigo = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

    // T2-07: público para que el controlador renueve la reserva al terminar cada pista (la sesión dura ~25 min)
    public int ReservaMinutos => configuracion.GetValue("Estudio:ReservaMinutos", 30);

    /// <summary>
    /// Si Estudio:CodigoAcceso está configurado, solo quien lo conoce (se entrega en el laboratorio)
    /// puede iniciar una sesión. Evita que personas ajenas al estudio llenen las cuotas.
    /// </summary>
    public bool CodigoAccesoValido(string? recibido)
    {
        var esperado = configuracion["Estudio:CodigoAcceso"];
        if (string.IsNullOrEmpty(esperado))
            return true;
        return recibido is not null &&
               CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(esperado), Encoding.UTF8.GetBytes(recibido));
    }

    public async Task<SesionExperimental> IniciarAsync(string consentimientoVersion, CancellationToken ct = default)
    {
        // Con EnableRetryOnFailure, EF Core exige que una transacción propia vaya dentro de la estrategia
        // de ejecución: si Neon corta la conexión, se reintenta la operación completa.
        var estrategia = db.Database.CreateExecutionStrategy();
        return await estrategia.ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear();
            await using var transaccion = await db.Database.BeginTransactionAsync(ct);

            // Candado de transacción: se libera solo al confirmar o deshacer
            await db.Database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock({ClaveCandado})", ct);

            var ahora = DateTime.UtcNow;

            // 1) Las reservas vencidas (participantes que abandonaron) dejan de ocupar plaza
            await db.Sesiones
                .Where(s => (s.Estado == EstadoSesion.Iniciada || s.Estado == EstadoSesion.EnAudio)
                            && s.ReservadaHastaUtc < ahora)
                .ExecuteUpdateAsync(u => u.SetProperty(s => s.Estado, EstadoSesion.Expirada), ct);

            // 2) T2-07: voces con audio certificado (las que escuchará el participante)
            var certificadas = await db.Condiciones
                .Where(c => c.Sha256 != null)
                .OrderBy(c => c.Id)
                .Select(c => c.Id)
                .ToArrayAsync(ct);

            if (certificadas.Length == 0)
                throw new EstimulosNoDisponiblesException();

            // 3) T2-07: ocupación de cada fila del cuadrado latino (solo sesiones con el mismo número de voces)
            var filas = CuadradoLatino.Williams(certificadas.Length);
            var n = certificadas.Length;
            var usadas = await db.Sesiones
                .Where(s => (s.Estado == EstadoSesion.Completada || s.Estado == EstadoSesion.Iniciada
                             || s.Estado == EstadoSesion.EnAudio)
                            && s.OrdenCondiciones.Length == n)
                .GroupBy(s => s.FilaOrden)
                .Select(g => new { Fila = g.Key, Total = g.Count() })
                .ToDictionaryAsync(x => x.Fila, x => x.Total, ct);
            var ocupacion = Enumerable.Range(0, filas.Count).ToDictionary(f => f, f => usadas.GetValueOrDefault(f));

            // 4) Fila menos usada; empate al azar. Se traduce a ids de condición.
            var fila = ElegirCondicion(ocupacion, Random.Shared);
            var orden = filas[fila].Select(i => certificadas[i]).ToArray();

            var sesion = new SesionExperimental
            {
                Id = Guid.CreateVersion7(),
                CodigoParticipante = GenerarCodigoParticipante(),
                CondicionVocalId = orden[0],
                OrdenCondiciones = orden,
                FilaOrden = fila,
                PistasEscuchadas = 0,
                Estado = EstadoSesion.Iniciada,
                ConsentimientoVersion = consentimientoVersion,
                ConsentimientoUtc = ahora,
                InicioUtc = ahora,
                ReservadaHastaUtc = ahora.AddMinutes(ReservaMinutos),
            };
            db.Sesiones.Add(sesion);
            await db.SaveChangesAsync(ct);
            await transaccion.CommitAsync(ct);
            return sesion;
        });
    }

    /// <summary>Devuelve una de las condiciones con menor ocupación, elegida al azar entre las empatadas.</summary>
    public static int ElegirCondicion(IReadOnlyDictionary<int, int> ocupacion, Random azar)
    {
        if (ocupacion.Count == 0)
            throw new ArgumentException("No hay condiciones disponibles.", nameof(ocupacion));
        var minimo = ocupacion.Values.Min();
        var candidatas = ocupacion.Where(x => x.Value == minimo).Select(x => x.Key).Order().ToArray();
        return candidatas[azar.Next(candidatas.Length)];
    }

    /// <summary>Código anónimo de la sesión, p. ej. MK-7QH2KX9P. No contiene datos del participante.</summary>
    public static string GenerarCodigoParticipante() =>
        "MK-" + RandomNumberGenerator.GetString(AlfabetoCodigo, 8);
}
