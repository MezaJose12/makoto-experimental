using Makoto.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Makoto.Api.Data.Configurations;

public class DatosParticipanteConfiguration : IEntityTypeConfiguration<DatosParticipante>
{
    public void Configure(EntityTypeBuilder<DatosParticipante> b)
    {
        b.ToTable("datos_participante");
        b.HasKey(d => d.SesionId);
        b.Property(d => d.RangoEdad).HasMaxLength(10).IsRequired();
        b.Property(d => d.Genero).HasMaxLength(30).IsRequired();
        b.Property(d => d.ExperienciaAsistentesVoz).HasMaxLength(30).IsRequired();

        b.HasOne(d => d.Sesion)
         .WithOne(s => s.DatosParticipante)
         .HasForeignKey<DatosParticipante>(d => d.SesionId)
         .OnDelete(DeleteBehavior.Cascade);
    }
}
