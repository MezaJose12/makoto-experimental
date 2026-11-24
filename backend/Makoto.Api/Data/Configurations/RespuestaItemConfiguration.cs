using Makoto.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Makoto.Api.Data.Configurations;

public class RespuestaItemConfiguration : IEntityTypeConfiguration<RespuestaItem>
{
    public void Configure(EntityTypeBuilder<RespuestaItem> b)
    {
        b.ToTable("respuestas_item");
        b.HasKey(r => new { r.EvaluacionId, r.ItemId });

        b.HasOne(r => r.Evaluacion)
         .WithMany(e => e.Respuestas)
         .HasForeignKey(r => r.EvaluacionId)
         .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(r => r.Item)
         .WithMany(i => i.Respuestas)
         .HasForeignKey(r => r.ItemId)
         .OnDelete(DeleteBehavior.Restrict);
    }
}
