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

    private int ReservaMinutos => configuracion.GetValue("Estudio:ReservaMinutos", 30);

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

            // 2) Ocupación de cada celda con audio certificado
            var ocupacion = await db.Condiciones
                .Where(c => c.Sha256 != null)
                .Select(c => new
                {
                    c.Id,
                    Ocupadas = c.Sesiones.Count(s => s.Estado == EstadoSesion.Completada
                                                    || s.Estado == EstadoSesion.Iniciada
                                                    || s.Estado == EstadoSesion.EnAudio)
                })
                .ToDictionaryAsync(x => x.Id, x => x.Ocupadas, ct);

            if (ocupacion.Count == 0)
                throw new EstimulosNoDisponiblesException();

            // 3) Celda menos ocupada; empate al azar
            var sesion = new SesionExperimental
            {
                Id = Guid.CreateVersion7(),
                CodigoParticipante = GenerarCodigoParticipante(),
                CondicionVocalId = ElegirCondicion(ocupacion, Random.Shared),
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
