using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GestaoRestaurante.Core.Domain.ValueObjects;
using GestaoRestaurante.Pagamentos.Domain.Entities;

namespace GestaoRestaurante.Pagamentos.Infrastructure.Persistence.Configurations;

public class CobrancaPlanoConfiguration : IEntityTypeConfiguration<CobrancaPlano>
{
    public void Configure(EntityTypeBuilder<CobrancaPlano> builder)
    {
        builder.ToTable("CobrancasPlano");

        builder.HasKey(cobranca => cobranca.Id);
        builder.Property(cobranca => cobranca.RestauranteId);

        builder.Property(cobranca => cobranca.Plano)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(cobranca => cobranca.Valor)
            .HasConversion(valor => valor.Valor, valor => Dinheiro.Criar(valor))
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(cobranca => cobranca.Dias).IsRequired();

        builder.Property(cobranca => cobranca.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(cobranca => cobranca.CriadoEm).IsRequired();
        builder.Property(cobranca => cobranca.PagaEm);

        builder.Property(cobranca => cobranca.IdSessaoGateway)
            .HasMaxLength(200);
    }
}
