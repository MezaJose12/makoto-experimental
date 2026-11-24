using Makoto.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Makoto.Api.Data.Configurations;

public class EventoTelemetriaConfiguration : IEntityTypeConfiguration<EventoTelemetria>
{
    public void Configure(EntityTypeBuilder<EventoTelemetria> b)
    {
        b.ToTable("eventos_telemetria");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).UseIdentityAlwaysColumn();
        b.Property(e => e.Metadatos).HasColumnType("jsonb");

        b.HasOne(e => e.Sesion)
         .WithMany(s => s.Eventos)
         .HasForeignKey(e => e.SesionId)
         .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(e => new { e.SesionId, e.Tipo });
    }
}
