using Microsoft.EntityFrameworkCore;

namespace Makoto.Api.Data;

/// <summary>
/// Al arrancar, copia a condiciones_vocales el F0 medido, la duración, la huella y la voz de cada
/// audio certificado. Si un audio falta, cambió o no se aprobó, su condición queda como pendiente.
/// </summary>
public static class SincronizadorCertificacion
{
    public static async Task<int> SincronizarAsync(MakotoDbContext db, string rutaAudios, ILogger logger,
                                                   CancellationToken ct = default)
    {
        var verificadas = CertificacionAudios.Cargar(rutaAudios);
        if (verificadas.Count == 0)
            logger.LogWarning("No hay certificación de audios en {Ruta}: las 6 condiciones quedan pendientes.", rutaAudios);

        var condiciones = await db.Condiciones.ToListAsync(ct);
        var certificadas = 0;
        foreach (var condicion in condiciones)
        {
            var v = verificadas.FirstOrDefault(x => x.Celda.Codigo == condicion.Codigo);
            var valida = v is { Valida: true } &&
                         string.Equals(Path.GetFileName(v.Celda.Archivo), condicion.Archivo, StringComparison.OrdinalIgnoreCase);

            if (v is not null && !valida)
                logger.LogWarning("Audio {Codigo} no certificado (aprobado: {Aprobada}, existe: {Existe}, huella correcta: {Huella}).",
                    condicion.Codigo, v.Celda.Aprobada, v.ArchivoExiste, v.HuellaCoincide);

            condicion.F0MedidoHz = valida ? Math.Round(v!.Celda.F0MedioHz, 1) : null;
            condicion.DuracionSegundos = valida ? Math.Round(v!.Celda.DuracionS, 2) : null;
            condicion.Sha256 = valida ? v!.Celda.Sha256.ToLowerInvariant() : null;
            condicion.VozTts = valida ? v!.Celda.Voz : null;
            if (valida) certificadas++;
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Audios certificados: {Certificadas} de {Total}.", certificadas, condiciones.Count);
        return certificadas;
    }
}
