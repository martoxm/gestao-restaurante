using MediatR;
using RestauranteEstoque.Application.UseCases.Produtos;
using RestauranteEstoque.Core.Mensageria;
using RestauranteEstoque.Core.Mensageria.Contratos;

namespace RestauranteEstoque.Api.Workers;

public sealed class AvisoAdminsConsumidor : ConsumidorFilaBase<AvisarAdminsRestauranteMensagem>
{
    private readonly ILogger<AvisoAdminsConsumidor> _logger;

    public AvisoAdminsConsumidor(ConexaoRabbitMq conexao, IServiceScopeFactory scopeFactory, ILogger<AvisoAdminsConsumidor> logger)
        : base(conexao, scopeFactory, logger)
    {
        _logger = logger;
    }

    protected override string Fila => Filas.AvisarAdmins;

    protected override async Task<bool> ProcessarAsync(AvisarAdminsRestauranteMensagem mensagem, Guid mensagemId, IServiceProvider servicos, CancellationToken cancellationToken)
    {
        if (mensagemId == Guid.Empty)
        {
            _logger.LogError("Aviso '{Assunto}' chegou sem MessageId.", mensagem.Assunto);
            return false;
        }

        var sender = servicos.GetRequiredService<ISender>();
        await sender.Send(new EncaminharAvisoAdminsCommand(mensagemId, mensagem.RestauranteId, mensagem.Assunto, mensagem.CorpoHtml), cancellationToken);

        return true;
    }
}
