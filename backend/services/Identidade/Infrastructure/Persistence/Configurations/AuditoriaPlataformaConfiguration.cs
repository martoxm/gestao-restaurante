using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GestaoRestaurante.Identidade.Domain.Entities;

namespace GestaoRestaurante.Identidade.Infrastructure.Persistence.Configurations;

public class AuditoriaPlataformaConfiguration : IEntityTypeConfiguration<AuditoriaPlataforma>
{
    public void Configure(EntityTypeBuilder<AuditoriaPlataforma> builder)
    {
        builder.ToTable("AuditoriasPlataforma");

        builder.HasKey(auditoria => auditoria.Id);

        builder.Property(auditoria => auditoria.SuperAdminId).IsRequired();
        builder.Property(auditoria => auditoria.EmailSuperAdmin).HasMaxLength(200).IsRequired();
        builder.Property(auditoria => auditoria.Acao).HasMaxLength(100).IsRequired();
        builder.Property(auditoria => auditoria.ValorAntes).HasMaxLength(500);
        builder.Property(auditoria => auditoria.ValorDepois).HasMaxLength(500);
        builder.Property(auditoria => auditoria.OcorridoEm).IsRequired();
    }
}
