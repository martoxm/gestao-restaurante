using GestaoRestaurante.Core.Application.Exceptions;
using GestaoRestaurante.Core.Persistencia.Mensageria;
using Microsoft.EntityFrameworkCore;
using GestaoRestaurante.Core.Application.Abstractions;
using GestaoRestaurante.Core.Mensageria;
using GestaoRestaurante.Vendas.Application.Abstractions.Services;
using GestaoRestaurante.Vendas.Domain.Entities;

namespace GestaoRestaurante.Vendas.Infrastructure.Persistence;

public class AppDbContext : DbContext, IApplicationDbContext
{
    private readonly IUsuarioAtualService _usuarioAtualService;

    public AppDbContext(DbContextOptions<AppDbContext> options, IUsuarioAtualService usuarioAtualService)
        : base(options)
    {
        _usuarioAtualService = usuarioAtualService;
    }

    private Guid? RestauranteIdAtual => _usuarioAtualService.RestauranteId;

    public DbSet<Venda> Vendas => Set<Venda>();
    public DbSet<ItemVenda> ItensVenda => Set<ItemVenda>();
    public DbSet<OutboxMensagem> OutboxMensagens => Set<OutboxMensagem>();
    public DbSet<MensagemProcessada> MensagensProcessadas => Set<MensagemProcessada>();
    public DbSet<ProdutoCatalogo> ProdutosCatalogo => Set<ProdutoCatalogo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("vendas");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        modelBuilder.ApplyConfiguration(new OutboxMensagemConfiguration());
        modelBuilder.ApplyConfiguration(new MensagemProcessadaConfiguration());

        modelBuilder.Entity<Venda>().HasQueryFilter(venda => venda.RestauranteId == RestauranteIdAtual);

        modelBuilder.Entity<ProdutoCatalogo>().HasQueryFilter(produto => produto.RestauranteId == RestauranteIdAtual);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConflitoConcorrenciaException(ex);
        }
        catch (DbUpdateException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException { Number: 2601 or 2627 })
        {
            throw new ConflitoConcorrenciaException(ex);
        }
    }
}
