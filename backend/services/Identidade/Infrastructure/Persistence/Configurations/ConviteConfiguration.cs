using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GestaoRestaurante.Identidade.Domain.Entities;

namespace GestaoRestaurante.Identidade.Infrastructure.Persistence.Configurations;

public class ConviteConfiguration : IEntityTypeConfiguration<Convite>
{
    public void Configure(EntityTypeBuilder<Convite> builder)
    {
        builder.ToTable("Convites");

        builder.HasKey(convite => convite.Id);

        builder.Property(convite => convite.RestauranteId).IsRequired();
        builder.Property(convite => convite.NomeRestaurante).HasMaxLength(150).IsRequired();
        builder.Property(convite => convite.Email).HasMaxLength(200).IsRequired();
        builder.Property(convite => convite.Role).HasMaxLength(50).IsRequired();

        builder.Property(convite => convite.TokenHash).HasMaxLength(64).IsRequired();
        builder.HasIndex(convite => convite.TokenHash).IsUnique();

        builder.Property(convite => convite.CriadoEm).IsRequired();
        builder.Property(convite => convite.ExpiraEm).IsRequired();
        builder.Property(convite => convite.AceitoEm);
        builder.Property(convite => convite.CanceladoEm);
    }
}
