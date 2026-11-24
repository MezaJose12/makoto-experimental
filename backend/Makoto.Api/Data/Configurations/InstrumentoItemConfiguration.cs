using Makoto.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Makoto.Api.Data.Configurations;

public class InstrumentoItemConfiguration : IEntityTypeConfiguration<InstrumentoItem>
{
    public void Configure(EntityTypeBuilder<InstrumentoItem> b)
    {
        b.ToTable("instrumento_items", t =>
            t.HasCheckConstraint("ck_instrumento_items_escala", "escala_min < escala_max"));
        b.HasKey(i => i.Id);
        b.Property(i => i.Instrumento).HasMaxLength(20).IsRequired();
        b.Property(i => i.Codigo).HasMaxLength(30).IsRequired();
        b.HasIndex(i => i.Codigo).IsUnique();
        b.Property(i => i.TextoEs).HasMaxLength(500).IsRequired();
        b.Property(i => i.Factor).HasMaxLength(50);
        // Sin semilla: los ítems se cargan cuando la traducción de PETS esté validada (T1-14)
    }
}
