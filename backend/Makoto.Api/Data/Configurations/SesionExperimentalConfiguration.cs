using Makoto.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Makoto.Api.Data.Configurations;

public class SesionExperimentalConfiguration : IEntityTypeConfiguration<SesionExperimental>
{
    public void Configure(EntityTypeBuilder<SesionExperimental> b)
    {
        b.ToTable("sesiones_experimentales");
        b.HasKey(s => s.Id);
        b.Property(s => s.CodigoParticipante).HasMaxLength(12).IsRequired();
        b.HasIndex(s => s.CodigoParticipante).IsUnique();
        b.Property(s => s.ConsentimientoVersion).HasMaxLength(20).IsRequired();

        b.HasOne(s => s.CondicionVocal)
         .WithMany(c => c.Sesiones)
         .HasForeignKey(s => s.CondicionVocalId)
         .OnDelete(DeleteBehavior.Restrict);

        // Para contar cuotas por celda y estado rápidamente (Asignador, T2-05)
        b.HasIndex(s => new { s.CondicionVocalId, s.Estado });
    }
}
