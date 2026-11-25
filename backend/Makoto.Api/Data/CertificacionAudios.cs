using System.Security.Cryptography;
using System.Text.Json;

namespace Makoto.Api.Data;

/// <summary>
/// Lee datos/audios/certificados/certificacion.json (lo genera T2-04 con Praat) y comprueba
/// que cada MP3 exista y que su huella SHA-256 coincida con la certificada.
/// </summary>
public static class CertificacionAudios
{
    public const string Carpeta = "certificados";
    public const string ArchivoCertificado = "certificacion.json";

    public sealed record CeldaCertificada(
        string Codigo, string Archivo, string Voz, decimal F0MedioHz, decimal DuracionS, string Sha256, bool Aprobada);

    public sealed record CeldaVerificada(CeldaCertificada Celda, bool ArchivoExiste, bool HuellaCoincide)
    {
        public bool Valida => Celda.Aprobada && ArchivoExiste && HuellaCoincide;
    }

    private sealed record Documento(bool Aprobada, List<CeldaCertificada> Celdas);

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>Devuelve una lista vacía si todavía no hay certificado.</summary>
    public static IReadOnlyList<CeldaVerificada> Cargar(string rutaAudios)
    {
        var carpeta = Path.Combine(rutaAudios, Carpeta);
        var ruta = Path.Combine(carpeta, ArchivoCertificado);
        if (!File.Exists(ruta))
            return [];

        var documento = JsonSerializer.Deserialize<Documento>(File.ReadAllText(ruta), Json)
                        ?? throw new InvalidDataException($"{ArchivoCertificado} está vacío.");

        return documento.Celdas.Select(celda =>
        {
            // Solo el nombre: un certificado alterado no puede apuntar fuera de la carpeta
            var mp3 = Path.Combine(carpeta, Path.GetFileName(celda.Archivo));
            if (!File.Exists(mp3))
                return new CeldaVerificada(celda, false, false);

            using var archivo = File.OpenRead(mp3);
            var huella = Convert.ToHexStringLower(SHA256.HashData(archivo));
            return new CeldaVerificada(celda, true,
                string.Equals(huella, celda.Sha256, StringComparison.OrdinalIgnoreCase));
        }).ToList();
    }
}
