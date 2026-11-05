using GestaoRestaurante.Vendas.Infrastructure.Persistence;
using GestaoRestaurante.Core.Persistencia.Mensageria;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using GestaoRestaurante.Core.Domain.ValueObjects;
using GestaoRestaurante.Core.Mensageria;
using GestaoRestaurante.Core.Mensageria.Contratos;
using GestaoRestaurante.Vendas.Application.UseCases.Vendas;
using GestaoRestaurante.Vendas.Domain.Entities;
using GestaoRestaurante.Vendas.Domain.Enums;
using GestaoRestaurante.Vendas.Infrastructure.Persistence.Repositories;
using GestaoRestaurante.Vendas.Tests.Common;

namespace GestaoRestaurante.Vendas.Tests.Handlers;

public class ProcessarResultadoPagamentoHandlerTests
{
    private static readonly Guid ProdutoId = Guid.NewGuid();

    private static async Task<(string NomeBanco, Guid VendaId)> CriarVendaAguardandoPagamentoAsync()
    {
        var nomeBanco = Guid.NewGuid().ToString();
        await using var context = TesteVendas.CriarContexto(nomeBanco: nomeBanco);
        var venda = Venda.Criar(TesteVendas.RestauranteId, 1, TesteVendas.Agora,
            [new NovoItemVenda(ProdutoId, "Coca-Cola", Dinheiro.Criar(5m), Quantidade.Criar(2))], TesteVendas.Token);
        venda.AguardarPagamento();
        context.Vendas.Add(venda);
        await context.SaveChangesAsync();
        return (nomeBanco, venda.Id);
    }

    private static async Task ProcessarAsync(string nomeBanco, Guid mensagemId, ResultadoPagamentoMensagem resultado)
    {
        var usuario = new FakeUsuarioAtualService();
        await using var context = TesteVendas.CriarContexto(usuario, nomeBanco);
        var handler = new ProcessarResultadoPagamentoHandler(
            new VendaRepository(context),
            new MensagemProcessadaRepository<AppDbContext>(context),
            new OutboxMensagemRepository<AppDbContext>(context),
            usuario,
            context,
            new FakeRelogio(),
            NullLogger<ProcessarResultadoPagamentoHandler>.Instance);
        await handler.Handle(new ProcessarResultadoPagamentoCommand(mensagemId, resultado), CancellationToken.None);
    }

    private static ResultadoPagamentoMensagem Resultado(Guid vendaId, bool aprovado, string? motivo = null) =>
        new(vendaId, TesteVendas.RestauranteId, Guid.NewGuid(), aprovado, motivo);

    [Fact]
    public async Task Handle_Aprovado_ConfirmaSemDevolverNada()
    {
        var (nomeBanco, vendaId) = await CriarVendaAguardandoPagamentoAsync();

        await ProcessarAsync(nomeBanco, Guid.NewGuid(), Resultado(vendaId, true));

        await using var context = TesteVendas.CriarContexto(nomeBanco: nomeBanco);
        Assert.Equal(StatusVenda.Confirmada, (await context.Vendas.SingleAsync()).Status);
        Assert.Empty(await context.OutboxMensagens.ToListAsync());
    }

    [Fact]
    public async Task Handle_Recusado_CancelaEPedeDevolucaoDoEstoque()
    {
        var (nomeBanco, vendaId) = await CriarVendaAguardandoPagamentoAsync();

        await ProcessarAsync(nomeBanco, Guid.NewGuid(), Resultado(vendaId, false, "Saldo insuficiente."));

        await using var context = TesteVendas.CriarContexto(nomeBanco: nomeBanco);
        var cancelada = await context.Vendas.SingleAsync();
        Assert.Equal(StatusVenda.Cancelada, cancelada.Status);
        Assert.Equal(MotivoCancelamento.PagamentoRecusado, cancelada.MotivoCancelamento);
        var devolucao = await context.OutboxMensagens.SingleAsync();
        Assert.Equal(Filas.DevolverEstoque, devolucao.Fila);
        Assert.Contains(ProdutoId.ToString(), devolucao.Conteudo);
        Assert.Contains("Saldo insuficiente.", devolucao.Conteudo);
    }

    [Fact]
    public async Task Handle_MesmaRecusaDuasVezes_PedeUmaDevolucaoSo()
    {
        var (nomeBanco, vendaId) = await CriarVendaAguardandoPagamentoAsync();
        var mensagemId = Guid.NewGuid();
        var resultado = Resultado(vendaId, false, "Saldo insuficiente.");

        await ProcessarAsync(nomeBanco, mensagemId, resultado);
        await ProcessarAsync(nomeBanco, mensagemId, resultado);

        await using var context = TesteVendas.CriarContexto(nomeBanco: nomeBanco);
        Assert.Equal(1, await context.OutboxMensagens.CountAsync());
    }

    [Fact]
    public async Task Handle_VendaQueNaoEstaAguardando_IgnoraEMarcaComoProcessada()
    {
        var (nomeBanco, vendaId) = await CriarVendaAguardandoPagamentoAsync();
        await ProcessarAsync(nomeBanco, Guid.NewGuid(), Resultado(vendaId, true));

        await ProcessarAsync(nomeBanco, Guid.NewGuid(), Resultado(vendaId, false, "Atrasada."));

        await using var context = TesteVendas.CriarContexto(nomeBanco: nomeBanco);
        Assert.Equal(StatusVenda.Confirmada, (await context.Vendas.SingleAsync()).Status);
        Assert.Equal(2, await context.MensagensProcessadas.CountAsync());
    }
}
