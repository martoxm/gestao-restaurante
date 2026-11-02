using Microsoft.EntityFrameworkCore;
using RestauranteEstoque.Core.Application.Abstractions;
using RestauranteEstoque.Identidade.Application.Abstractions.Services;
using RestauranteEstoque.Identidade.Domain.Entities;

namespace RestauranteEstoque.Identidade.Tests.Common;

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
