using System.Security.Cryptography;
using System.Text.Json;
using Makoto.Api.Data;

namespace Makoto.Tests;

public sealed class CertificacionAudiosTests : IDisposable
{
    private readonly string _audios = Directory.CreateTempSubdirectory("makoto-audios-").FullName;
    private string Carpeta => Path.Combine(_audios, CertificacionAudios.Carpeta);

    public void Dispose() => Directory.Delete(_audios, recursive: true);

    private string CrearMp3(string nombre, string contenido = "audio de prueba")
    {
        Directory.CreateDirectory(Carpeta);
        var ruta = Path.Combine(Carpeta, nombre);
        File.WriteAllText(ruta, contenido);
        return Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(ruta)));
    }

    private void CrearCertificado(params object[] celdas) =>
        File.WriteAllText(Path.Combine(Carpeta, CertificacionAudios.ArchivoCertificado),
            JsonSerializer.Serialize(new { version = 1, aprobada = true, celdas }));

    private static object Celda(string archivo, string sha256, bool aprobada = true) => new
    {
        codigo = "F_GRAVE", archivo, voz = "es-EC-AndreaNeural", genero = "Femenino",
        f0ObjetivoHz = 175, f0MedioHz = 174.6, duracionS = 37.42, sha256, aprobada
    };

    [Fact]
    public void SinCertificado_DevuelveListaVacia() =>
        Assert.Empty(CertificacionAudios.Cargar(_audios));

    [Fact]
    public void AudioIntacto_EsValido_YLeeLosValoresMedidos()
    {
        CrearCertificado(Celda("f_grave.mp3", CrearMp3("f_grave.mp3")));

        var v = Assert.Single(CertificacionAudios.Cargar(_audios));

        Assert.True(v.Valida);
        Assert.Equal(174.6m, v.Celda.F0MedioHz);
        Assert.Equal(37.42m, v.Celda.DuracionS);
        Assert.Equal("es-EC-AndreaNeural", v.Celda.Voz);
    }

    [Fact]
    public void AudioModificado_DespuesDeCertificar_NoEsValido()
    {
        var huella = CrearMp3("f_grave.mp3");
        CrearCertificado(Celda("f_grave.mp3", huella));
        File.AppendAllText(Path.Combine(Carpeta, "f_grave.mp3"), "cambio");

        var v = Assert.Single(CertificacionAudios.Cargar(_audios));

        Assert.True(v.ArchivoExiste);
        Assert.False(v.HuellaCoincide);
        Assert.False(v.Valida);
    }

    [Fact]
    public void AudioFaltante_O_NoAprobado_NoEsValido()
    {
        Directory.CreateDirectory(Carpeta);
        CrearCertificado(Celda("no_existe.mp3", "abc"), Celda("f_grave.mp3", CrearMp3("f_grave.mp3"), aprobada: false));

        var celdas = CertificacionAudios.Cargar(_audios);

        Assert.False(celdas[0].ArchivoExiste);
        Assert.True(celdas[1].HuellaCoincide);
        Assert.All(celdas, c => Assert.False(c.Valida));
    }

    [Fact]
    public void RutaManipulada_NoSaleDeLaCarpetaDeCertificados()
    {
        var huella = CrearMp3("f_grave.mp3");
        CrearCertificado(Celda("../../f_grave.mp3", huella));

        var v = Assert.Single(CertificacionAudios.Cargar(_audios));

        Assert.True(v.Valida);   // se resolvió dentro de certificados/, no dos carpetas arriba
    }
}
