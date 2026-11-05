using GestaoRestaurante.Core.Application.Exceptions;
using GestaoRestaurante.Domain.Entities;
using GestaoRestaurante.Core.Domain.ValueObjects;
using GestaoRestaurante.Infrastructure.Persistence.Repositories;
using GestaoRestaurante.Tests.Integration.Fixtures;

namespace GestaoRestaurante.Tests.Integration.Repositories;

[Collection(nameof(SqlServerCollection))]
public sealed class ProdutoRepositoryTests(SqlServerFixture fixture)
{
    private static readonly DateTime CriadoEm = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [DockerFact]
    public async Task Adicionar_DeveGravarELerOProdutoComOsValueObjects()
    {
        if (!fixture.EstaDisponivel)
            return;

        var categoria = DadosTeste.CriarCategoria();
        var produto = Produto.Criar(DadosTeste.RestauranteId, "Arroz", "Tipo 1", Dinheiro.Criar(12.34m), categoria, CriadoEm, Quantidade.Criar(7));

        await using (var contexto = fixture.CriarContexto())
        {
            contexto.Categorias.Add(categoria);
            new ProdutoRepository(contexto).Adicionar(produto);
            await contexto.SaveChangesAsync();
        }

        await using var contextoLeitura = fixture.CriarContexto();
        var lido = await new ProdutoRepository(contextoLeitura).ObterPorIdAsync(produto.Id, CancellationToken.None);

        Assert.NotNull(lido);
        Assert.Equal("Arroz", lido.Nome);
        Assert.Equal(Dinheiro.Criar(12.34m), lido.Preco);
        Assert.Equal(Quantidade.Criar(7), lido.QuantidadeEmEstoque);
        Assert.NotNull(lido.Categoria);
        Assert.Equal("Grãos", lido.Categoria.Nome);
    }

    [DockerFact]
    public async Task Atualizar_DuasSaidasAoMesmoTempo_SegundaDeveDarConflito()
    {
        if (!fixture.EstaDisponivel)
            return;

        var categoria = DadosTeste.CriarCategoria();
        var produto = Produto.Criar(DadosTeste.RestauranteId, "Feijão", null, Dinheiro.Criar(9m), categoria, CriadoEm, Quantidade.Criar(10));

        await using (var contexto = fixture.CriarContexto())
        {
            contexto.Categorias.Add(categoria);
            new ProdutoRepository(contexto).Adicionar(produto);
            await contexto.SaveChangesAsync();
        }

        await using var contextoA = fixture.CriarContexto();
        await using var contextoB = fixture.CriarContexto();
        var produtoA = await new ProdutoRepository(contextoA).ObterPorIdAsync(produto.Id, CancellationToken.None);
        var produtoB = await new ProdutoRepository(contextoB).ObterPorIdAsync(produto.Id, CancellationToken.None);

        produtoA!.DiminuirEstoque(Quantidade.Criar(2));
        new ProdutoRepository(contextoA).Atualizar(produtoA);
        await contextoA.SaveChangesAsync();

        produtoB!.DiminuirEstoque(Quantidade.Criar(2));
        new ProdutoRepository(contextoB).Atualizar(produtoB);
        await Assert.ThrowsAsync<ConflitoConcorrenciaException>(() => contextoB.SaveChangesAsync());

        await using var contextoLeitura = fixture.CriarContexto();
        var lido = await new ProdutoRepository(contextoLeitura).ObterPorIdAsync(produto.Id, CancellationToken.None);
        Assert.Equal(Quantidade.Criar(8), lido!.QuantidadeEmEstoque);
        Assert.NotNull(lido.VersaoLinha);
    }
}
