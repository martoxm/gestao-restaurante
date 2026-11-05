using MediatR;
using GestaoRestaurante.Core.Mensageria;
using GestaoRestaurante.Core.Mensageria.Contratos;
using GestaoRestaurante.Email.Application.UseCases.Emails;

namespace GestaoRestaurante.Email.Api.Workers;

public sealed class EnvioEmailConsumidor : ConsumidorFilaBase<EnviarEmailMensagem>
{
    private readonly ILogger<EnvioEmailConsumidor> _logger;

    public EnvioEmailConsumidor(ConexaoRabbitMq conexao, IServiceScopeFactory scopeFactory, ILogger<EnvioEmailConsumidor> logger)
        : base(conexao, scopeFactory, logger)
    {
        _logger = logger;
    }

    protected override string Fila => Filas.Email;

    protected override async Task<bool> ProcessarAsync(EnviarEmailMensagem mensagem, Guid mensagemId, IServiceProvider servicos, CancellationToken cancellationToken)
    {
        if (mensagemId == Guid.Empty)
        {
            _logger.LogError("Pedido de e-mail '{Assunto}' chegou sem MessageId.", mensagem.Assunto);
            return false;
        }

        var sender = servicos.GetRequiredService<ISender>();
        return await sender.Send(new EnviarEmailCommand(mensagemId, mensagem.Destinatarios, mensagem.Assunto, mensagem.CorpoHtml), cancellationToken);
    }
}
