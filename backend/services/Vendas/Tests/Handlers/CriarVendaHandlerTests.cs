using GestaoRestaurante.Vendas.Infrastructure.Persistence;
using GestaoRestaurante.Core.Persistencia.Mensageria;
using System.Text.Json;
using GestaoRestaurante.Core.Mensageria;
using GestaoRestaurante.Core.Mensageria.Contratos;
using GestaoRestaurante.Core.Application.Exceptions;
using GestaoRestaurante.Vendas.Application.UseCases.Vendas;
using GestaoRestaurante.Vendas.Contracts.Common;
using GestaoRestaurante.Vendas.Contracts.Vendas;
using GestaoRestaurante.Vendas.Domain.Entities;
using GestaoRestaurante.Vendas.Infrastructure.Persistence.Repositories;
using GestaoRestaurante.Vendas.Tests.Common;

namespace GestaoRestaurante.Vendas.Tests.Handlers;

public class CriarVendaHandlerTests
{
    [Fact]
    public async Task Handle_CopiaNomeEPrecoDoCatalogoLocal_EGravaAVenda()
    {
        var nomeBanco = Guid.NewGuid().ToString();
        ProdutoCatalogo coca;
        await using (var preparo = TesteVendas.CriarContexto(nomeBanco: nomeBanco))
            coca = await TesteVendas.AdicionarProdutoAsync(preparo, "Coca-Cola", 5m);

        await using (var context = TesteVendas.CriarContexto(nomeBanco: nomeBanco))
        {
            var handler = new CriarVendaHandler(new VendaRepository(context), new OutboxMensagemRepository<AppDbContext>(context), new ProdutoCatalogoRepository(context), context, new FakeUsuarioAtualService { RestauranteId = TesteVendas.RestauranteId }, new FakeRelogio());
            await handler.Handle(new CriarVendaCommand([new ItemVendaRequest(coca.Id, 3)], TesteVendas.Token), CancellationToken.None);
        }

        await using var leitura = TesteVendas.CriarContexto(nomeBanco: nomeBanco);
        var (vendas, _) = await new VendaRepository(leitura).ListarPaginadoAsync(1, 10, DirecaoOrdenacao.Desc, CancellationToken.None);
        var venda = Assert.Single(vendas);
        var item = Assert.Single(venda.Itens);
        Assert.Equal("Coca-Cola", item.NomeProduto);
        Assert.Equal(5m, item.PrecoUnitario.Valor);
        Assert.Equal(15m, venda.Total.Valor);
        Assert.Equal(TesteVendas.Agora, venda.CriadoEm);
    }

    [Fact]
    public async Task Handle_VendaValida_GravaPedidoDeBaixaNoOutbox()
    {
        var nomeBanco = Guid.NewGuid().ToString();
        ProdutoCatalogo coca;
        await using (var preparo = TesteVendas.CriarContexto(nomeBanco: nomeBanco))
            coca = await TesteVendas.AdicionarProdutoAsync(preparo, "Coca-Cola", 5m);
        Guid vendaId;

        await using (var context = TesteVendas.CriarContexto(nomeBanco: nomeBanco))
        {
            var handler = new CriarVendaHandler(new VendaRepository(context), new OutboxMensagemRepository<AppDbContext>(context), new ProdutoCatalogoRepository(context), context, new FakeUsuarioAtualService { RestauranteId = TesteVendas.RestauranteId }, new FakeRelogio());
            vendaId = await handler.Handle(new CriarVendaCommand([new ItemVendaRequest(coca.Id, 3)], TesteVendas.Token), CancellationToken.None);
        }

        await using var leitura = TesteVendas.CriarContexto(nomeBanco: nomeBanco);
        var pendentes = await new OutboxMensagemRepository<AppDbContext>(leitura).ListarPendentesAsync(10, TesteVendas.Agora, CancellationToken.None);
        var linha = Assert.Single(pendentes);
        Assert.Equal(Filas.BaixarEstoque, linha.Fila);
        Assert.Equal(nameof(BaixarEstoqueMensagem), linha.Tipo);

        var pedido = JsonSerializer.Deserialize<BaixarEstoqueMensagem>(linha.Conteudo)!;
        Assert.Equal(vendaId, pedido.VendaId);
        Assert.Equal(TesteVendas.RestauranteId, pedido.RestauranteId);
        var item = Assert.Single(pedido.Itens);
        Assert.Equal(coca.Id, item.ProdutoId);
        Assert.Equal(3, item.Quantidade);
    }

    [Fact]
    public async Task Handle_ProdutoForaDoCatalogo_LancaNaoEncontrado()
    {
        await using var context = TesteVendas.CriarContexto();
        var handler = new CriarVendaHandler(new VendaRepository(context), new OutboxMensagemRepository<AppDbContext>(context), new ProdutoCatalogoRepository(context), context, new FakeUsuarioAtualService { RestauranteId = TesteVendas.RestauranteId }, new FakeRelogio());

        await Assert.ThrowsAsync<NaoEncontradoException>(() =>
            handler.Handle(new CriarVendaCommand([new ItemVendaRequest(Guid.NewGuid(), 1)], TesteVendas.Token), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_TokenSemRestaurante_LancaSessaoInvalida()
    {
        await using var context = TesteVendas.CriarContexto();
        var handler = new CriarVendaHandler(new VendaRepository(context), new OutboxMensagemRepository<AppDbContext>(context), new ProdutoCatalogoRepository(context), context, new FakeUsuarioAtualService(), new FakeRelogio());

        await Assert.ThrowsAsync<SessaoInvalidaException>(() =>
            handler.Handle(new CriarVendaCommand([new ItemVendaRequest(Guid.NewGuid(), 1)], TesteVendas.Token), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ProdutoExcluidoNoCatalogo_LancaNaoEncontrado()
    {
        await using var context = TesteVendas.CriarContexto();
        var produto = await TesteVendas.AdicionarProdutoAsync(context, "Coca-Cola", 5m, excluido: true);
        var handler = new CriarVendaHandler(new VendaRepository(context), new OutboxMensagemRepository<AppDbContext>(context), new ProdutoCatalogoRepository(context), context, new FakeUsuarioAtualService { RestauranteId = TesteVendas.RestauranteId }, new FakeRelogio());

        await Assert.ThrowsAsync<NaoEncontradoException>(() =>
            handler.Handle(new CriarVendaCommand([new ItemVendaRequest(produto.Id, 1)], TesteVendas.Token), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ProdutoDeOutroRestaurante_LancaNaoEncontrado()
    {
        await using var context = TesteVendas.CriarContexto();
        var produto = await TesteVendas.AdicionarProdutoAsync(context, "Coca-Cola", 5m, restauranteId: Guid.NewGuid());
        var handler = new CriarVendaHandler(new VendaRepository(context), new OutboxMensagemRepository<AppDbContext>(context), new ProdutoCatalogoRepository(context), context, new FakeUsuarioAtualService { RestauranteId = TesteVendas.RestauranteId }, new FakeRelogio());

        await Assert.ThrowsAsync<NaoEncontradoException>(() =>
            handler.Handle(new CriarVendaCommand([new ItemVendaRequest(produto.Id, 1)], TesteVendas.Token), CancellationToken.None));
    }

    private static async Task<Guid> CriarVendaAsync(string nomeBanco, Guid restauranteId, Guid produtoId)
    {
        await using var context = TesteVendas.CriarContexto(new FakeUsuarioAtualService { RestauranteId = restauranteId }, nomeBanco);
        var handler = new CriarVendaHandler(new VendaRepository(context), new OutboxMensagemRepository<AppDbContext>(context), new ProdutoCatalogoRepository(context), context, new FakeUsuarioAtualService { RestauranteId = restauranteId }, new FakeRelogio());
        return await handler.Handle(new CriarVendaCommand([new ItemVendaRequest(produtoId, 1)], TesteVendas.Token), CancellationToken.None);
    }

    [Fact]
    public async Task Handle_VendasSeguidas_NumeraEmSequencia()
    {
        var nomeBanco = Guid.NewGuid().ToString();
        ProdutoCatalogo coca;
        await using (var preparo = TesteVendas.CriarContexto(nomeBanco: nomeBanco))
            coca = await TesteVendas.AdicionarProdutoAsync(preparo, "Coca-Cola", 5m);

        var primeira = await CriarVendaAsync(nomeBanco, TesteVendas.RestauranteId, coca.Id);
        var segunda = await CriarVendaAsync(nomeBanco, TesteVendas.RestauranteId, coca.Id);

        await using var leitura = TesteVendas.CriarContexto(nomeBanco: nomeBanco);
        var repositorio = new VendaRepository(leitura);
        Assert.Equal(1, (await repositorio.ObterPorIdAsync(primeira, CancellationToken.None))!.Numero);
        Assert.Equal(2, (await repositorio.ObterPorIdAsync(segunda, CancellationToken.None))!.Numero);
    }

    [Fact]
    public async Task Handle_RestaurantesDiferentes_NumeracaoIndependente()
    {
        var nomeBanco = Guid.NewGuid().ToString();
        var outroRestaurante = Guid.NewGuid();
        ProdutoCatalogo cocaA, cocaB;
        await using (var preparo = TesteVendas.CriarContexto(nomeBanco: nomeBanco))
        {
            cocaA = await TesteVendas.AdicionarProdutoAsync(preparo, "Coca-Cola", 5m);
            cocaB = await TesteVendas.AdicionarProdutoAsync(preparo, "Coca-Cola", 5m, restauranteId: outroRestaurante);
        }

        await CriarVendaAsync(nomeBanco, TesteVendas.RestauranteId, cocaA.Id);
        await CriarVendaAsync(nomeBanco, TesteVendas.RestauranteId, cocaA.Id);
        var vendaDoOutro = await CriarVendaAsync(nomeBanco, outroRestaurante, cocaB.Id);

        await using var leitura = TesteVendas.CriarContexto(new FakeUsuarioAtualService { RestauranteId = outroRestaurante }, nomeBanco);
        var venda = await new VendaRepository(leitura).ObterPorIdAsync(vendaDoOutro, CancellationToken.None);
        Assert.Equal(1, venda!.Numero);
    }

    [Fact]
    public async Task Handle_PedidoDeBaixa_LevaONumeroDaVenda()
    {
        var nomeBanco = Guid.NewGuid().ToString();
        ProdutoCatalogo coca;
        await using (var preparo = TesteVendas.CriarContexto(nomeBanco: nomeBanco))
            coca = await TesteVendas.AdicionarProdutoAsync(preparo, "Coca-Cola", 5m);

        await CriarVendaAsync(nomeBanco, TesteVendas.RestauranteId, coca.Id);

        await using var leitura = TesteVendas.CriarContexto(nomeBanco: nomeBanco);
        var linha = Assert.Single(await new OutboxMensagemRepository<AppDbContext>(leitura).ListarPendentesAsync(10, TesteVendas.Agora, CancellationToken.None));
        var pedido = JsonSerializer.Deserialize<BaixarEstoqueMensagem>(linha.Conteudo)!;
        Assert.Equal(1, pedido.NumeroVenda);
    }
}
