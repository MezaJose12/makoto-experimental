using System.Text;
using Makoto.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Makoto.Api.Data;

/// <summary>
/// Contexto de EF Core: la única puerta entre el back-end y PostgreSQL.
/// Tablas y columnas en snake_case para poder consultarlas en SQL sin comillas.
/// </summary>
public class MakotoDbContext(DbContextOptions<MakotoDbContext> options) : DbContext(options)
{
    public DbSet<CondicionVocal> Condiciones => Set<CondicionVocal>();
    public DbSet<SesionExperimental> Sesiones => Set<SesionExperimental>();
    public DbSet<DatosParticipante> DatosParticipantes => Set<DatosParticipante>();
    public DbSet<EventoTelemetria> Eventos => Set<EventoTelemetria>();
    public DbSet<Evaluacion> Evaluaciones => Set<Evaluacion>();
    public DbSet<InstrumentoItem> InstrumentoItems => Set<InstrumentoItem>();
    public DbSet<RespuestaItem> Respuestas => Set<RespuestaItem>();
    public DbSet<Administrador> Administradores => Set<Administrador>();

    protected override void ConfigureConventions(ModelConfigurationBuilder b)
    {
        // Enums guardados como texto legible (p. ej. 'Completada'), no como números
        b.Properties<GeneroVocal>().HaveConversion<string>().HaveMaxLength(20);
        b.Properties<NivelFrecuencia>().HaveConversion<string>().HaveMaxLength(20);
        b.Properties<EstadoSesion>().HaveConversion<string>().HaveMaxLength(20);
        b.Properties<TipoEvento>().HaveConversion<string>().HaveMaxLength(20);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MakotoDbContext).Assembly);

        foreach (var entidad in modelBuilder.Model.GetEntityTypes())
            foreach (var propiedad in entidad.GetProperties())
                propiedad.SetColumnName(ASnakeCase(propiedad.Name));
    }

    /// <summary>CondicionVocalId → condicion_vocal_id</summary>
    public static string ASnakeCase(string nombre)
    {
        var sb = new StringBuilder(nombre.Length + 8);
        for (var i = 0; i < nombre.Length; i++)
        {
            var c = nombre[i];
            if (char.IsUpper(c) && i > 0 && (char.IsLower(nombre[i - 1]) || char.IsDigit(nombre[i - 1]) ||
                (i + 1 < nombre.Length && char.IsLower(nombre[i + 1]))))
                sb.Append('_');
            sb.Append(char.ToLowerInvariant(c));
        }
        return sb.ToString();
    }
}
