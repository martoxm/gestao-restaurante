using Microsoft.EntityFrameworkCore;
using GestaoRestaurante.Core.Domain;
using GestaoRestaurante.Pagamentos.Application.UseCases.Pagamentos;
using GestaoRestaurante.Pagamentos.Infrastructure.Persistence;
using GestaoRestaurante.Pagamentos.Infrastructure.Persistence.Repositories;
using GestaoRestaurante.Pagamentos.Tests.Common;

namespace GestaoRestaurante.Pagamentos.Tests.Handlers;

public class CriarPagamentoHandlerTests
{
    private static CriarPagamentoHandler CriarHandler(AppDbContext context) =>
        new(new PagamentoRepository(context), context, new FakeUsuarioAtualService { RestauranteId = TestePagamentos.RestauranteId }, new FakeRelogio());

    [Fact]
    public async Task Handle_GravaPagamentoPendente()
    {
        using var context = TestePagamentos.CriarContexto();
        var command = new CriarPagamentoCommand("chave-1", Guid.NewGuid(), 42.5m, "tok_aprovado");

        var resposta = await CriarHandler(context).Handle(command, CancellationToken.None);

        Assert.Equal("Pendente", resposta.Status);
        var salvo = await context.Pagamentos.SingleAsync();
        Assert.Equal(resposta.Id, salvo.Id);
        Assert.Equal(42.5m, salvo.Valor.Valor);
    }

    [Fact]
    public async Task Handle_MesmaChaveMesmoPedido_DevolveOMesmoPagamentoSemCriarOutro()
    {
        using var context = TestePagamentos.CriarContexto();
        var command = new CriarPagamentoCommand("chave-1", Guid.NewGuid(), 42.5m, "tok_aprovado");

        var primeira = await CriarHandler(context).Handle(command, CancellationToken.None);
        var segunda = await CriarHandler(context).Handle(command, CancellationToken.None);

        Assert.Equal(primeira.Id, segunda.Id);
        Assert.Equal(1, await context.Pagamentos.CountAsync());
    }

    [Fact]
    public async Task Handle_MesmaChavePedidoDiferente_LancaDomainException()
    {
        using var context = TestePagamentos.CriarContexto();
        var vendaId = Guid.NewGuid();
        await CriarHandler(context).Handle(new CriarPagamentoCommand("chave-1", vendaId, 42.5m, "tok_aprovado"), CancellationToken.None);

        await Assert.ThrowsAsync<DomainException>(() =>
            CriarHandler(context).Handle(new CriarPagamentoCommand("chave-1", vendaId, 99m, "tok_aprovado"), CancellationToken.None));
    }
}
