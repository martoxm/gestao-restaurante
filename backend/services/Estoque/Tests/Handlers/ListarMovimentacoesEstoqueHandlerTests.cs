using RestauranteEstoque.Application.UseCases.MovimentacoesEstoque;
using RestauranteEstoque.Core.Domain.ValueObjects;
using RestauranteEstoque.Domain.Entities;
using RestauranteEstoque.Domain.Enums;
using RestauranteEstoque.Domain.Services;
using RestauranteEstoque.Tests.Common;

namespace RestauranteEstoque.Tests.Handlers;

public class ListarMovimentacoesEstoqueHandlerTests
{
    private static readonly DateTime Dia1 = new(2026, 10, 1, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Dia2 = new(2026, 10, 2, 18, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Dia3 = new(2026, 10, 3, 9, 0, 0, DateTimeKind.Utc);

    private static async Task<(TestDbContext Context, Produto Arroz, Produto Feijao)> MontarCenarioAsync()
    {
        var context = TestDbContextFactory.Create();
        var categoria = DadosTeste.CriarCategoria();
        var arroz = Produto.Criar(DadosTeste.RestauranteId, "Arroz", null, Dinheiro.Criar(10m), categoria, Dia1, Quantidade.Criar(20));
        var feijao = Produto.Criar(DadosTeste.RestauranteId, "Feijão", null, Dinheiro.Criar(8m), categoria, Dia1, Quantidade.Criar(20));
        context.Categorias.Add(categoria);
        context.Produtos.AddRange(arroz, feijao);
        context.MovimentacoesEstoque.AddRange(
            MovimentadorEstoque.Movimentar(arroz, TipoMovimentacaoEstoque.Entrada, Quantidade.Criar(5), Dia1, "Compra"),
            MovimentadorEstoque.Movimentar(arroz, TipoMovimentacaoEstoque.Saida, Quantidade.Criar(2), Dia2, null),
            MovimentadorEstoque.Movimentar(feijao, TipoMovimentacaoEstoque.Saida, Quantidade.Criar(1), Dia3, null));
        await context.SaveChangesAsync();
        return (context, arroz, feijao);
    }

    private static ListarMovimentacoesEstoqueHandler CriarHandler(TestDbContext context) =>
        new(new FakeMovimentacaoEstoqueRepository(context));

    [Fact]
    public async Task Handle_SemFiltro_DeveListarDaMaisNovaParaAMaisAntigaComNomeDoProduto()
    {
        var (context, _, _) = await MontarCenarioAsync();
        using var _context = context;

        var resultado = await CriarHandler(context).Handle(new ListarMovimentacoesEstoqueQuery(), CancellationToken.None);

        Assert.Equal(3, resultado.TotalItens);
        Assert.Equal(new[] { Dia3, Dia2, Dia1 }, resultado.Itens.Select(item => item.DataMovimentacao));
        Assert.Equal("Feijão", resultado.Itens[0].NomeProduto);
        Assert.Equal("Saida", resultado.Itens[0].Tipo);
    }

    [Fact]
    public async Task Handle_FiltrandoPorProdutoETipo_DeveTrazerSoAsQueBatem()
    {
        var (context, arroz, _) = await MontarCenarioAsync();
        using var _context = context;

        var resultado = await CriarHandler(context).Handle(
            new ListarMovimentacoesEstoqueQuery(ProdutoId: arroz.Id, Tipo: "saida"),
            CancellationToken.None);

        var item = Assert.Single(resultado.Itens);
        Assert.Equal(2, item.Quantidade);
    }

    [Fact]
    public async Task Handle_DataFim_DeveIncluirODiaInteiro()
    {
        var (context, _, _) = await MontarCenarioAsync();
        using var _context = context;

        var resultado = await CriarHandler(context).Handle(
            new ListarMovimentacoesEstoqueQuery(DataInicio: Dia2.Date, DataFim: Dia2.Date),
            CancellationToken.None);

        Assert.Equal(Dia2, Assert.Single(resultado.Itens).DataMovimentacao);
    }
}
