using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Makoto.Tests;

/// <summary>
/// Levanta la API en memoria, en modo Production, con una base de datos inexistente.
/// Comprueba lo que REV-TEC 01 exige sin necesitar PostgreSQL.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public const string OrigenPermitido = "http://localhost:5173";

    public ApiFactory()
    {
        Environment.SetEnvironmentVariable("DATABASE_URL", null);
        Environment.SetEnvironmentVariable("ConnectionStrings__Default",
            "Host=127.0.0.1;Port=1;Database=inexistente;Username=x;Password=x");
        Environment.SetEnvironmentVariable("Cors__Origenes__0", OrigenPermitido);
        Environment.SetEnvironmentVariable("Database__AplicarMigraciones", "false");
        Environment.SetEnvironmentVariable("Admin__ClaveTemporal", ClaveAdmin);
        // T2-05: con un código de estudio configurado, iniciar sesión sin él debe rechazarse
        Environment.SetEnvironmentVariable("Estudio__CodigoAcceso", CodigoEstudio);
    }

    public const string CodigoEstudio = "codigo-de-prueba";

    public const string ClaveAdmin = "clave-de-prueba";

    protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.UseEnvironment("Production");
}

public class ApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Health_Responde200_SinTocarLaBaseDeDatos()
    {
        var response = await factory.CreateClient().GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task OpenApi_NoSeExpone_EnProduccion()
    {
        var response = await factory.CreateClient().GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Cors_PermiteLaPwa_YRechazaOtrosOrigenes()
    {
        var client = factory.CreateClient();

        var permitida = new HttpRequestMessage(HttpMethod.Get, "/health");
        permitida.Headers.Add("Origin", ApiFactory.OrigenPermitido);
        var ok = await client.SendAsync(permitida);

        var ajena = new HttpRequestMessage(HttpMethod.Get, "/health");
        ajena.Headers.Add("Origin", "https://sitio-ajeno.example");
        var rechazada = await client.SendAsync(ajena);

        Assert.Equal(ApiFactory.OrigenPermitido,
            ok.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.False(rechazada.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("clave-incorrecta")]
    public async Task Admin_SinClaveValida_Responde401(string? clave)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/cuotas");
        if (clave is not null) request.Headers.Add("X-Admin-Key", clave);

        var response = await factory.CreateClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task EndpointsPendientes_Responden501_ConProblemDetails()
    {
        var response = await factory.CreateClient().PostAsync($"/api/sesiones/{Guid.NewGuid()}/eventos",
            new StringContent("""{"tipo":"AudioStart"}""", System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.NotImplemented, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Validacion_RechazaDatosInvalidos_Con400()
    {
        var response = await factory.CreateClient().PostAsync("/api/sesiones",
            new StringContent("""{"consentimientoVersion":""}""", System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // T2-05: sin consentimiento no se crea la sesión (se valida antes de tocar la base de datos)
    [Fact]
    public async Task Iniciar_SinConsentimiento_Responde400()
    {
        var response = await factory.CreateClient().PostAsync("/api/sesiones", new StringContent(
            $$"""{"consentimientoAceptado":false,"consentimientoVersion":"v1","codigoAcceso":"{{ApiFactory.CodigoEstudio}}"}""",
            System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // T2-05: con código de estudio configurado, un código incorrecto o ausente responde 403
    [Theory]
    [InlineData(null)]
    [InlineData("otro-codigo")]
    public async Task Iniciar_SinCodigoDelEstudio_Responde403(string? codigo)
    {
        var cuerpo = codigo is null
            ? """{"consentimientoAceptado":true,"consentimientoVersion":"v1"}"""
            : $$"""{"consentimientoAceptado":true,"consentimientoVersion":"v1","codigoAcceso":"{{codigo}}"}""";

        var response = await factory.CreateClient().PostAsync("/api/sesiones",
            new StringContent(cuerpo, System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
