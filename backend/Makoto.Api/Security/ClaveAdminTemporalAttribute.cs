using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Makoto.Api.Security;

/// <summary>
/// Protección TEMPORAL del panel hasta el login JWT (T2-20): exige la cabecera X-Admin-Key
/// con el valor de Admin:ClaveTemporal. Si la clave no está configurada, nadie entra.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class ClaveAdminTemporalAttribute : Attribute, IAuthorizationFilter
{
    public const string Cabecera = "X-Admin-Key";

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var esperada = context.HttpContext.RequestServices
            .GetRequiredService<IConfiguration>()["Admin:ClaveTemporal"];
        var recibida = context.HttpContext.Request.Headers[Cabecera].ToString();

        if (string.IsNullOrEmpty(esperada) ||
            !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(esperada), Encoding.UTF8.GetBytes(recibida)))
            context.Result = new UnauthorizedResult();
    }
}
