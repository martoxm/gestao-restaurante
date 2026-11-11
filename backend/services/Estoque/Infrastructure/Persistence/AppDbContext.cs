using GestaoRestaurante.Domain.Common;
using GestaoRestaurante.Core.Application.Exceptions;
using GestaoRestaurante.Core.Persistencia.Mensageria;
using GestaoRestaurante.Core.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using GestaoRestaurante.Application.Abstractions.Services;
using GestaoRestaurante.Core.Mensageria;
using GestaoRestaurante.Domain.Entities;

namespace GestaoRestaurante.Infrastructure.Persistence;

public class AppDbContext : DbContext, IApplicationDbContext
{
    private readonly IUsuarioAtualService _usuarioAtualService;

    public AppDbContext(DbContextOptions<AppDbContext> options, IUsuarioAtualService usuarioAtualService)
        : base(options)
    {
        _usuarioAtualService = usuarioAtualService;
    }

    private Guid? RestauranteIdAtual => _usuarioAtualService.RestauranteId;

    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<MovimentacaoEstoque> MovimentacoesEstoque => Set<MovimentacaoEstoque>();
    public DbSet<Fornecedor> Fornecedores => Set<Fornecedor>();

    public DbSet<OutboxMensagem> OutboxMensagens => Set<OutboxMensagem>();
    public DbSet<MensagemProcessada> MensagensProcessadas => Set<MensagemProcessada>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("estoque");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        modelBuilder.ApplyConfiguration(new OutboxMensagemConfiguration());
        modelBuilder.ApplyConfiguration(new MensagemProcessadaConfiguration());

        modelBuilder.Entity<Produto>().HasQueryFilter(FiltrosConsulta.Restaurante, produto => produto.RestauranteId == RestauranteIdAtual);
        modelBuilder.Entity<Produto>().HasQueryFilter(FiltrosConsulta.NaoExcluido, produto => produto.ExcluidoEm == null);
        modelBuilder.Entity<Categoria>().HasQueryFilter(FiltrosConsulta.Restaurante, categoria => categoria.RestauranteId == RestauranteIdAtual);
        modelBuilder.Entity<Categoria>().HasQueryFilter(FiltrosConsulta.NaoExcluido, categoria => categoria.ExcluidoEm == null);
        modelBuilder.Entity<Fornecedor>().HasQueryFilter(FiltrosConsulta.Restaurante, fornecedor => fornecedor.RestauranteId == RestauranteIdAtual);
        modelBuilder.Entity<Fornecedor>().HasQueryFilter(FiltrosConsulta.NaoExcluido, fornecedor => fornecedor.ExcluidoEm == null);
        modelBuilder.Entity<MovimentacaoEstoque>().HasQueryFilter(FiltrosConsulta.Restaurante, movimentacao => movimentacao.RestauranteId == RestauranteIdAtual);
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
    }
}
