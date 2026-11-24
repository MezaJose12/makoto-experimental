using System.Data.Common;
using Makoto.Api.Data;

namespace Makoto.Tests;

public class ConnectionStringResolverTests
{
    private static DbConnectionStringBuilder Parse(string cs) => new() { ConnectionString = cs };

    [Fact]
    public void UriDeNeon_SeConvierteConSslYContrasenaEscapada()
    {
        var url = "postgresql://neondb_owner:p%40ss%3Bw0rd@ep-ejemplo-pooler.us-east-2.aws.neon.tech/neondb?sslmode=require&channel_binding=require";

        var cs = Parse(ConnectionStringResolver.Resolve(url, null));

        Assert.Equal("ep-ejemplo-pooler.us-east-2.aws.neon.tech", cs["Host"]);
        Assert.Equal("5432", cs["Port"].ToString());
        Assert.Equal("neondb", cs["Database"]);
        Assert.Equal("neondb_owner", cs["Username"]);
        Assert.Equal("p@ss;w0rd", cs["Password"]);
        Assert.Equal("Require", cs["SSL Mode"]);
        Assert.Equal("Require", cs["Channel Binding"]);
    }

    [Fact]
    public void UriConEsquemaPostgresYPuerto_RespetaElPuerto()
    {
        var cs = Parse(ConnectionStringResolver.Resolve("postgres://u:p@db.local:6543/makoto", null));

        Assert.Equal("6543", cs["Port"].ToString());
        Assert.Equal("Require", cs["SSL Mode"]);   // sin sslmode, se exige SSL por defecto
    }

    [Fact]
    public void SslModeDisable_SeTraduce()
    {
        var cs = Parse(ConnectionStringResolver.Resolve("postgresql://u:p@localhost/makoto?sslmode=disable", null));

        Assert.Equal("Disable", cs["SSL Mode"]);
    }

    [Fact]
    public void DatabaseUrl_TienePrioridadSobreLaConfiguracion()
    {
        var cs = Parse(ConnectionStringResolver.Resolve(
            "postgresql://u:p@neon.example/makoto", "Host=postgres;Database=local"));

        Assert.Equal("neon.example", cs["Host"]);
    }

    [Fact]
    public void SinDatabaseUrl_UsaLaCadenaConfigurada()
    {
        const string local = "Host=postgres;Port=5432;Database=makoto;Username=makoto;Password=x";

        Assert.Equal(local, ConnectionStringResolver.Resolve(null, local));
        Assert.Equal(local, ConnectionStringResolver.Resolve("   ", local));
    }

    [Fact]
    public void SinNingunaCadena_LanzaErrorClaro()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => ConnectionStringResolver.Resolve(null, ""));

        Assert.Contains("DATABASE_URL", ex.Message);
    }

    [Theory]
    [InlineData("mysql://u:p@host/db")]
    [InlineData("postgresql://host/db")]
    [InlineData("postgresql://u:p@host/")]
    public void UriInvalida_LanzaError(string url)
    {
        Assert.Throws<InvalidOperationException>(() => ConnectionStringResolver.Resolve(url, null));
    }
}
