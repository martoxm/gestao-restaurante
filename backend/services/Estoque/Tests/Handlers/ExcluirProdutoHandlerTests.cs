using System.Text.Json;
using GestaoRestaurante.Core.Mensageria.Contratos;
using Microsoft.EntityFrameworkCore;
using GestaoRestaurante.Core.Application.Exceptions;
using GestaoRestaurante.Application.UseCases.Produtos;
using GestaoRestaurante.Core.Domain;
using GestaoRestaurante.Domain.Common;
using GestaoRestaurante.Domain.Entities;
using GestaoRestaurante.Domain.Enums;
using GestaoRestaurante.Core.Domain.ValueObjects;
using GestaoRestaurante.Tests.Common;

namespace GestaoRestaurante.Tests.Handlers;

public class ExcluirProdutoHandlerTests
{
    private static readonly DateTime CriadoEm = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Handle_Produto_DeveSumirDasConsultasMasContinuarNoBanco()
    {
        using var context = TestDbContextFactory.Create();
        var produto = await CriarProdutoAsync(context);

        await CriarHandler(context).Handle(new ExcluirProdutoCommand(produto.Id), CancellationToken.None);
        context.ChangeTracker.Clear();

        Assert.False(await context.Produtos.AnyAsync(p => p.Id == produto.Id));
        var noBanco = await context.Produtos.IgnoreQueryFilters([FiltrosConsulta.NaoExcluido]).SingleAsync(p => p.Id == produto.Id);
        Assert.NotNull(noBanco.ExcluidoEm);
    }

    [Fact]
    public async Task Handle_ProdutoComMovimentacoes_DeveExcluirEManterOHistorico()
    {
        using var context = TestDbContextFactory.Create();
        var produto = await CriarProdutoAsync(context);
        context.MovimentacoesEstoque.Add(MovimentacaoEstoque.Criar(
            DadosTeste.RestauranteId, produto.Id, TipoMovimentacaoEstoque.Entrada, Quantidade.Criar(5), CriadoEm));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        await CriarHandler(context).Handle(new ExcluirProdutoCommand(produto.Id), CancellationToken.None);
        context.ChangeTracker.Clear();

        var historico = await new FakeMovimentacaoEstoqueRepository(context)
            .ListarPaginadoAsync(1, 10, null, null, null, null, CancellationToken.None);
        var item = Assert.Single(historico.Itens);
        Assert.Equal("Arroz", item.Produto!.Nome);
    }

    [Fact]
    public async Task Handle_ProdutoInexistente_DeveLancarNaoEncontradoException()
    {
        using var context = TestDbContextFactory.Create();

        await Assert.ThrowsAsync<NaoEncontradoException>(() =>
            CriarHandler(context).Handle(new ExcluirProdutoCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ProdutoExcluido_DeveAvisarOCatalogoComExcluidoTrue()
    {
        using var context = TestDbContextFactory.Create();
        var produto = await CriarProdutoAsync(context);
        var outbox = new FakeOutboxMensagemRepository();

        await CriarHandler(context, outbox).Handle(new ExcluirProdutoCommand(produto.Id), CancellationToken.None);

        var mensagem = JsonSerializer.Deserialize<ProdutoCatalogoMensagem>(Assert.Single(outbox.Mensagens).Conteudo)!;
        Assert.Equal(produto.Id, mensagem.ProdutoId);
        Assert.True(mensagem.Excluido);
    }

    [Fact]
    public async Task Reativar_ProdutoExcluido_DeveVoltarEAvisarOCatalogoComExcluidoFalse()
    {
        using var context = TestDbContextFactory.Create();
        var produto = await CriarProdutoAsync(context);
        await CriarHandler(context).Handle(new ExcluirProdutoCommand(produto.Id), CancellationToken.None);
        context.ChangeTracker.Clear();
        var outbox = new FakeOutboxMensagemRepository();

        await CriarReativar(context, outbox).Handle(new ReativarProdutoCommand(produto.Id), CancellationToken.None);
        context.ChangeTracker.Clear();

        Assert.True(await context.Produtos.AnyAsync(p => p.Id == produto.Id));
        var mensagem = JsonSerializer.Deserialize<ProdutoCatalogoMensagem>(Assert.Single(outbox.Mensagens).Conteudo)!;
        Assert.False(mensagem.Excluido);
    }

    [Fact]
    public async Task Reativar_ProdutoNaoExcluido_DeveLancarNaoEncontradoException()
    {
        using var context = TestDbContextFactory.Create();
        var produto = await CriarProdutoAsync(context);

        await Assert.ThrowsAsync<NaoEncontradoException>(() =>
            CriarReativar(context).Handle(new ReativarProdutoCommand(produto.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Reativar_ComCategoriaExcluida_DeveLancarDomainException()
    {
        using var context = TestDbContextFactory.Create();
        var produto = await CriarProdutoAsync(context);
        await CriarHandler(context).Handle(new ExcluirProdutoCommand(produto.Id), CancellationToken.None);
        context.ChangeTracker.Clear();
        var categoria = await context.Categorias.SingleAsync();
        categoria.Excluir(CriadoEm);
        context.Categorias.Update(categoria);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        await Assert.ThrowsAsync<DomainException>(() =>
            CriarReativar(context).Handle(new ReativarProdutoCommand(produto.Id), CancellationToken.None));
    }

    private static ExcluirProdutoHandler CriarHandler(TestDbContext context, FakeOutboxMensagemRepository? outbox = null) =>
        new(new FakeProdutoRepository(context), context, outbox ?? new FakeOutboxMensagemRepository(), new FakeRelogio());

    private static ReativarProdutoHandler CriarReativar(TestDbContext context, FakeOutboxMensagemRepository? outbox = null) =>
        new(new FakeProdutoRepository(context), new FakeCategoriaRepository(context), context, outbox ?? new FakeOutboxMensagemRepository(), new FakeRelogio());

    private static async Task<Produto> CriarProdutoAsync(TestDbContext context)
    {
        var categoria = Categoria.Criar(DadosTeste.RestauranteId, "Grãos", CriadoEm);
        var produto = Produto.Criar(DadosTeste.RestauranteId, "Arroz", null, Dinheiro.Criar(10m), categoria, CriadoEm);
        context.Categorias.Add(categoria);
        context.Produtos.Add(produto);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        return produto;
    }
}
