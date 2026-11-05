using GestaoRestaurante.Core.Persistencia.Mensageria;
using Microsoft.EntityFrameworkCore;
using GestaoRestaurante.Core.Application.Abstractions;
using GestaoRestaurante.Core.Mensageria;
using GestaoRestaurante.Pagamentos.Application.Abstractions.Services;
using GestaoRestaurante.Pagamentos.Domain.Entities;

namespace GestaoRestaurante.Pagamentos.Infrastructure.Persistence;

public class AppDbContext : DbContext, IApplicationDbContext
{
    private readonly IUsuarioAtualService _usuarioAtualService;

    public AppDbContext(DbContextOptions<AppDbContext> options, IUsuarioAtualService usuarioAtualService)
        : base(options)
    {
        _usuarioAtualService = usuarioAtualService;
    }

    private Guid? RestauranteIdAtual => _usuarioAtualService.RestauranteId;

    public DbSet<Pagamento> Pagamentos => Set<Pagamento>();

    public DbSet<CobrancaPlano> CobrancasPlano => Set<CobrancaPlano>();
    public DbSet<OutboxMensagem> OutboxMensagens => Set<OutboxMensagem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("pagamentos");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        modelBuilder.ApplyConfiguration(new OutboxMensagemConfiguration());

        modelBuilder.Entity<Pagamento>().HasQueryFilter(pagamento => pagamento.RestauranteId == RestauranteIdAtual);
        modelBuilder.Entity<CobrancaPlano>().HasQueryFilter(cobranca => cobranca.RestauranteId == RestauranteIdAtual);
    }
}
