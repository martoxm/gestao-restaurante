using Microsoft.EntityFrameworkCore;
using GestaoRestaurante.Core.Application.Abstractions;
using GestaoRestaurante.Identidade.Application.Abstractions.Services;
using GestaoRestaurante.Identidade.Domain.Entities;

namespace GestaoRestaurante.Identidade.Tests.Common;

public class TestDbContext : DbContext, IApplicationDbContext
{
    private readonly IUsuarioAtualService _usuarioAtualService;

    public TestDbContext(DbContextOptions<TestDbContext> options, IUsuarioAtualService usuarioAtualService)
        : base(options)
    {
        _usuarioAtualService = usuarioAtualService;
    }

    public DbSet<Restaurante> Restaurantes => Set<Restaurante>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Convite> Convites => Set<Convite>();

    public DbSet<AuditoriaPlataforma> AuditoriasPlataforma => Set<AuditoriaPlataforma>();

    private Guid? RestauranteIdAtual => _usuarioAtualService.RestauranteId;

    public FakeTransacao? UltimaTransacao { get; private set; }

    public async Task<T> ExecutarEmTransacaoAsync<T>(Func<CancellationToken, Task<T>> operacao, CancellationToken cancellationToken)
    {
        UltimaTransacao = new FakeTransacao();
        try
        {
            var resultado = await operacao(cancellationToken);
            UltimaTransacao.Confirmar();
            return resultado;
        }
        finally
        {
            UltimaTransacao.Descartar();
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Restaurante>().HasQueryFilter(restaurante => restaurante.Id == RestauranteIdAtual);
        modelBuilder.Entity<Convite>().HasQueryFilter(convite => convite.RestauranteId == RestauranteIdAtual);
    }
}

public static class TestDbContextFactory
{
    public static TestDbContext Create(IUsuarioAtualService? usuarioAtualService = null)
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TestDbContext(options, usuarioAtualService ?? new FakeUsuarioAtualService());
    }
}
