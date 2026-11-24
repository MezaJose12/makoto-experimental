using Makoto.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Makoto.Api.Data.Configurations;

public class EvaluacionConfiguration : IEntityTypeConfiguration<Evaluacion>
{
    public void Configure(EntityTypeBuilder<Evaluacion> b)
    {
        b.ToTable("evaluaciones", t =>
            t.HasCheckConstraint("ck_evaluaciones_naturalidad", "naturalidad BETWEEN 1 AND 7"));
        b.HasKey(e => e.Id);
        b.Property(e => e.GeneroPercibido).HasMaxLength(20).IsRequired();
        b.Property(e => e.TonoPercibido).HasMaxLength(20).IsRequired();

        // 1:1 — una sesión tiene como máximo una evaluación (índice único en sesion_id)
        b.HasOne(e => e.Sesion)
         .WithOne(s => s.Evaluacion)
         .HasForeignKey<Evaluacion>(e => e.SesionId)
         .OnDelete(DeleteBehavior.Cascade);
    }
}
