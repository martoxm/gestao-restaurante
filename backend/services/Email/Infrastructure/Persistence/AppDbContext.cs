using GestaoRestaurante.Core.Persistencia.Mensageria;
using Microsoft.EntityFrameworkCore;
using GestaoRestaurante.Core.Mensageria;
using GestaoRestaurante.Email.Application.Abstractions.Services;

namespace GestaoRestaurante.Email.Infrastructure.Persistence;

public class AppDbContext : DbContext, IApplicationDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<MensagemProcessada> MensagensProcessadas => Set<MensagemProcessada>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("email");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        modelBuilder.ApplyConfiguration(new MensagemProcessadaConfiguration());
    }
}
