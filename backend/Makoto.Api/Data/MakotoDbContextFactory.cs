using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Makoto.Api.Data;

/// <summary>
/// Solo para las herramientas de diseño (dotnet ef migrations add).
/// Generar una migración no se conecta a la base de datos: la cadena solo indica el proveedor.
/// La API real nunca usa esta clase.
/// </summary>
public class MakotoDbContextFactory : IDesignTimeDbContextFactory<MakotoDbContext>
{
    public MakotoDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<MakotoDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=makoto;Username=makoto;Password=solo-diseno")
            .Options;

        return new MakotoDbContext(options);
    }
}