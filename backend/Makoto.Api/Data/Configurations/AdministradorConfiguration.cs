using Makoto.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Makoto.Api.Data.Configurations;

public class AdministradorConfiguration : IEntityTypeConfiguration<Administrador>
{
    public void Configure(EntityTypeBuilder<Administrador> b)
    {
        b.ToTable("administradores");
        b.HasKey(a => a.Id);
        b.Property(a => a.Usuario).HasMaxLength(50).IsRequired();
        b.HasIndex(a => a.Usuario).IsUnique();
        b.Property(a => a.PasswordHash).HasMaxLength(200).IsRequired();
        b.Property(a => a.Rol).HasMaxLength(20).IsRequired();
    }
}
