namespace Makoto.Api.Models;

/// <summary>Cuenta del panel. La contraseña se guarda solo como hash (login JWT en T2-20).</summary>
public class Administrador
{
    public int Id { get; set; }
    public string Usuario { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Rol { get; set; } = "Admin";
    public int IntentosFallidos { get; set; }
    public DateTime? BloqueadoHastaUtc { get; set; }
}
