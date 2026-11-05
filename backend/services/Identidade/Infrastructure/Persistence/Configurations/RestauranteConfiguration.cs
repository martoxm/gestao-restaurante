using GestaoRestaurante.Core.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GestaoRestaurante.Identidade.Domain.Entities;

namespace GestaoRestaurante.Identidade.Infrastructure.Persistence.Configurations;

public class RestauranteConfiguration : IEntityTypeConfiguration<Restaurante>
{
    public void Configure(EntityTypeBuilder<Restaurante> builder)
    {
        builder.ToTable("Restaurantes");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Nome)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(r => r.CriadoEm)
            .IsRequired();

        builder.Property(r => r.Plano)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(r => r.TesteTerminaEm);

        builder.Property(r => r.UltimoAvisoFimTesteDias);

        builder.Property(r => r.PlanoValidoAte);
        builder.Property(r => r.AssinadoEm);
        builder.Property(r => r.UltimoPagamentoEm);

        builder.Property(r => r.Ativo)
            .IsRequired();
    }
}
