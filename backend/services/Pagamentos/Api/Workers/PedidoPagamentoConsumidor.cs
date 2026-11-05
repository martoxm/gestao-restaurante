using MediatR;
using GestaoRestaurante.Core.Application.Abstractions;
using GestaoRestaurante.Core.Mensageria;
using GestaoRestaurante.Core.Mensageria.Contratos;
using GestaoRestaurante.Pagamentos.Application.UseCases.Pagamentos;

namespace GestaoRestaurante.Pagamentos.Api.Workers;

public sealed class PedidoPagamentoConsumidor : ConsumidorFilaBase<ProcessarPagamentoMensagem>
{   
    public PedidoPagamentoConsumidor(ConexaoRabbitMq conexao, IServiceScopeFactory scopeFactory, ILogger<PedidoPagamentoConsumidor> logger)
        : base(conexao, scopeFactory, logger)
    {
    }

    protected override string Fila => Filas.ProcessarPagamento;

    public static string ChaveDaVenda(Guid vendaId) => $"venda-{vendaId}";

    protected override async Task<bool> ProcessarAsync(ProcessarPagamentoMensagem mensagem, Guid mensagemId, IServiceProvider servicos, CancellationToken cancellationToken)
    {
        servicos.GetRequiredService<IDefinidorRestauranteAtual>().Definir(mensagem.RestauranteId);

        var sender = servicos.GetRequiredService<ISender>();
        await sender.Send(
            new CriarPagamentoCommand(ChaveDaVenda(mensagem.VendaId), mensagem.VendaId, mensagem.Valor, mensagem.TokenCartao),
            cancellationToken);

        return true;
    }
}
