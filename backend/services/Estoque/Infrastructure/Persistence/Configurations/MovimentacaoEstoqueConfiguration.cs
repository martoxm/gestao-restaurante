using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GestaoRestaurante.Domain.Entities;
using GestaoRestaurante.Core.Domain.ValueObjects;

namespace GestaoRestaurante.Infrastructure.Persistence.Configurations;

public class MovimentacaoEstoqueConfiguration : IEntityTypeConfiguration<MovimentacaoEstoque>
{
    public void Configure(EntityTypeBuilder<MovimentacaoEstoque> builder)
    {
        builder.ToTable("MovimentacoesEstoque");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Tipo)
            .IsRequired();

        builder.Property(s => s.Quantidade)
            .HasConversion(quantidade => quantidade.Valor, valor => Quantidade.Criar(valor))
            .IsRequired();

        builder.Property(s => s.DataMovimentacao)
            .IsRequired();

        builder.Property(s => s.Observacoes)
            .HasMaxLength(500);

        builder.Property(s => s.RestauranteId);

        builder.HasOne(s => s.Produto)
            .WithMany()
            .HasForeignKey(s => s.ProdutoId)
            .IsRequired();
    }
}
