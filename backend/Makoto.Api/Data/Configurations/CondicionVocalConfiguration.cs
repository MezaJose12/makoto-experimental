using Makoto.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Makoto.Api.Data.Configurations;

public class CondicionVocalConfiguration : IEntityTypeConfiguration<CondicionVocal>
{
    public void Configure(EntityTypeBuilder<CondicionVocal> b)
    {
        b.ToTable("condiciones_vocales");
        b.HasKey(c => c.Id);
        b.Property(c => c.Id).ValueGeneratedNever();
        b.Property(c => c.Codigo).HasMaxLength(20).IsRequired();
        b.HasIndex(c => c.Codigo).IsUnique();
        b.Property(c => c.F0ObjetivoHz).HasPrecision(5, 1);
        b.Property(c => c.F0MedidoHz).HasPrecision(5, 1);
        b.Property(c => c.DuracionSegundos).HasPrecision(5, 2);
        b.Property(c => c.Archivo).HasMaxLength(100).IsRequired();
        b.Property(c => c.Sha256).HasMaxLength(64);
        b.Property(c => c.VozTts).HasMaxLength(100);

        // Semilla: las 6 celdas del diseño 2×3 (objetos anónimos: HasData no admite navegaciones)
        b.HasData(CatalogoCondiciones.Celdas.Select(c => new
        {
            c.Id, c.Codigo, c.Genero, c.Nivel, c.F0ObjetivoHz, c.Archivo
        }));
    }
}
