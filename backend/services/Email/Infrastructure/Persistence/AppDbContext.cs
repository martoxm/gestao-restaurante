using RestauranteEstoque.Core.Persistencia.Mensageria;
using Microsoft.EntityFrameworkCore;
using RestauranteEstoque.Core.Mensageria;
using RestauranteEstoque.Email.Application.Abstractions.Services;

namespace RestauranteEstoque.Email.Infrastructure.Persistence;

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
