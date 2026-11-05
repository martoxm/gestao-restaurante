using MediatR;
using GestaoRestaurante.Core.Mensageria;
using GestaoRestaurante.Core.Mensageria.Contratos;
using GestaoRestaurante.Identidade.Application.UseCases.Restaurantes;

namespace GestaoRestaurante.Identidade.Api.Workers;

public sealed class PlanoPagoConsumidor : ConsumidorFilaBase<PlanoPagoMensagem>
{
    public PlanoPagoConsumidor(ConexaoRabbitMq conexao, IServiceScopeFactory scopeFactory, ILogger<PlanoPagoConsumidor> logger)
        : base(conexao, scopeFactory, logger)
    {
    }

    protected override string Fila => Filas.PlanoPago;

    protected override async Task<bool> ProcessarAsync(PlanoPagoMensagem mensagem, Guid mensagemId, IServiceProvider servicos, CancellationToken cancellationToken)
    {
        var sender = servicos.GetRequiredService<ISender>();
        await sender.Send(new AplicarPagamentoPlanoCommand(mensagem), cancellationToken);
        return true;
    }
}
