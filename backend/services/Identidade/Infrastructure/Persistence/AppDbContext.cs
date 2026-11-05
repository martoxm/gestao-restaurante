using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using GestaoRestaurante.Core.Application.Abstractions;
using GestaoRestaurante.Core.Mensageria;
using GestaoRestaurante.Core.Persistencia.Mensageria;
using GestaoRestaurante.Identidade.Application.Abstractions.Services;
using GestaoRestaurante.Identidade.Domain.Entities;
using GestaoRestaurante.Identidade.Infrastructure.Identity;

namespace GestaoRestaurante.Identidade.Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>, IApplicationDbContext
{
    private readonly IUsuarioAtualService _usuarioAtualService;

    public AppDbContext(DbContextOptions<AppDbContext> options, IUsuarioAtualService usuarioAtualService)
        : base(options)
    {
        _usuarioAtualService = usuarioAtualService;
    }

    private Guid? RestauranteIdAtual => _usuarioAtualService.RestauranteId;

    public Task<T> ExecutarEmTransacaoAsync<T>(Func<CancellationToken, Task<T>> operacao, CancellationToken cancellationToken)
    {
        var estrategia = Database.CreateExecutionStrategy();

        return estrategia.ExecuteAsync(async ct =>
        {
            ChangeTracker.Clear();
            await using var transacao = await Database.BeginTransactionAsync(ct);
            var resultado = await operacao(ct);
            await transacao.CommitAsync(ct);
            return resultado;
        }, cancellationToken);
    }

    public DbSet<Restaurante> Restaurantes => Set<Restaurante>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Convite> Convites => Set<Convite>();
    public DbSet<OutboxMensagem> OutboxMensagens => Set<OutboxMensagem>();

    public DbSet<AuditoriaPlataforma> AuditoriasPlataforma => Set<AuditoriaPlataforma>();

    public DbSet<NotificacaoPlataforma> NotificacoesPlataforma => Set<NotificacaoPlataforma>();

    public DbSet<MensagemProcessada> MensagensProcessadas => Set<MensagemProcessada>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("identidade");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        modelBuilder.ApplyConfiguration(new OutboxMensagemConfiguration());
        modelBuilder.ApplyConfiguration(new MensagemProcessadaConfiguration());

        modelBuilder.Entity<Restaurante>().HasQueryFilter(restaurante => restaurante.Id == RestauranteIdAtual);

        modelBuilder.Entity<Convite>().HasQueryFilter(convite => convite.RestauranteId == RestauranteIdAtual);
    }
}
