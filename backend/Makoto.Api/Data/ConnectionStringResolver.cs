using System.Data.Common;

namespace Makoto.Api.Data;

/// <summary>
/// Decide qué cadena de conexión usa la API.
/// 1) DATABASE_URL (formato URI, p. ej. la que entrega Neon) → se convierte al formato de Npgsql con SSL.
/// 2) Si no existe, ConnectionStrings:Default (Docker Compose o desarrollo local).
/// No depende de Npgsql para poder probarse sin base de datos.
/// </summary>
public static class ConnectionStringResolver
{
    public static string Resolve(string? databaseUrl, string? configuredConnectionString)
    {
        if (!string.IsNullOrWhiteSpace(databaseUrl))
            return FromUri(databaseUrl.Trim());

        if (!string.IsNullOrWhiteSpace(configuredConnectionString))
            return configuredConnectionString.Trim();

        throw new InvalidOperationException(
            "No hay cadena de conexión: defina DATABASE_URL o ConnectionStrings__Default.");
    }

    public static string FromUri(string databaseUrl)
    {
        if (!Uri.TryCreate(databaseUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != "postgres" && uri.Scheme != "postgresql"))
            throw new InvalidOperationException("DATABASE_URL debe empezar por postgres:// o postgresql://.");

        var userInfo = uri.UserInfo.Split(':', 2);
        var database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/'));
        if (string.IsNullOrEmpty(userInfo[0]) || string.IsNullOrEmpty(database))
            throw new InvalidOperationException("DATABASE_URL debe incluir usuario y nombre de base de datos.");

        var query = ParseQuery(uri.Query);

        // DbConnectionStringBuilder escapa comillas, ';' y '=' en contraseñas.
        var builder = new DbConnectionStringBuilder
        {
            ["Host"] = uri.Host,
            ["Port"] = uri.Port > 0 ? uri.Port : 5432,
            ["Database"] = database,
            ["Username"] = Uri.UnescapeDataString(userInfo[0]),
            ["SSL Mode"] = query.TryGetValue("sslmode", out var ssl) ? ToNpgsqlSslMode(ssl) : "Require"
        };
        if (userInfo.Length > 1)
            builder["Password"] = Uri.UnescapeDataString(userInfo[1]);
        if (query.TryGetValue("channel_binding", out var channel))
            builder["Channel Binding"] = ToPascal(channel);

        return builder.ConnectionString;
    }

    private static Dictionary<string, string> ParseQuery(string query) =>
        query.TrimStart('?')
             .Split('&', StringSplitOptions.RemoveEmptyEntries)
             .Select(p => p.Split('=', 2))
             .Where(p => p.Length == 2)
             .ToDictionary(p => p[0].ToLowerInvariant(), p => Uri.UnescapeDataString(p[1]),
                           StringComparer.OrdinalIgnoreCase);

    private static string ToNpgsqlSslMode(string value) => value.ToLowerInvariant() switch
    {
        "disable" => "Disable",
        "allow" => "Allow",
        "prefer" => "Prefer",
        "verify-ca" => "VerifyCA",
        "verify-full" => "VerifyFull",
        _ => "Require"
    };

    private static string ToPascal(string value) =>
        value.Length == 0 ? value : char.ToUpperInvariant(value[0]) + value[1..].ToLowerInvariant();
}
