using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GestaoRestaurante.Identidade.Domain.Entities;

namespace GestaoRestaurante.Identidade.Infrastructure.Persistence.Configurations;

public class NotificacaoPlataformaConfiguration : IEntityTypeConfiguration<NotificacaoPlataforma>
{
    public void Configure(EntityTypeBuilder<NotificacaoPlataforma> builder)
    {
        builder.ToTable("NotificacoesPlataforma");

        builder.HasKey(notificacao => notificacao.Id);

        builder.Property(notificacao => notificacao.Tipo).HasMaxLength(50).IsRequired();
        builder.Property(notificacao => notificacao.Titulo).HasMaxLength(150).IsRequired();
        builder.Property(notificacao => notificacao.Mensagem).HasMaxLength(500).IsRequired();
        builder.Property(notificacao => notificacao.Chave).HasMaxLength(150);
        builder.Property(notificacao => notificacao.OcorridoEm).IsRequired();
    }
}
