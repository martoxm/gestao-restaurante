using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using GestaoRestaurante.Core.Application.Abstractions;
using GestaoRestaurante.Core.Mensageria;

namespace GestaoRestaurante.Core.Persistencia.Mensageria;

public sealed class PublicacaoOutboxWorker<TContext> : BackgroundService
    where TContext : DbContext
{
    private static readonly TimeSpan Intervalo = TimeSpan.FromSeconds(15);
    private const int QuantidadePorRodada = 50;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IPublicadorMensagens _publicador;
    private readonly ILogger<PublicacaoOutboxWorker<TContext>> _logger;

    public PublicacaoOutboxWorker(IServiceScopeFactory scopeFactory, IPublicadorMensagens publicador, ILogger<PublicacaoOutboxWorker<TContext>> logger)
    {
        _scopeFactory = scopeFactory;
        _publicador = publicador;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PublicarPendentesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao processar o outbox de mensagens.");
            }

            await Task.Delay(Intervalo, stoppingToken);
        }
    }

    private async Task PublicarPendentesAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var outbox = scope.ServiceProvider.GetRequiredService<IOutboxMensagemRepository>();
        var context = scope.ServiceProvider.GetRequiredService<TContext>();
        var relogio = scope.ServiceProvider.GetRequiredService<IRelogio>();

        var pendentes = await outbox.ListarPendentesAsync(QuantidadePorRodada, relogio.AgoraUtc, ct);

        foreach (var mensagem in pendentes)
        {
            try
            {
                await _publicador.PublicarAsync(mensagem.Fila, mensagem.Tipo, mensagem.Conteudo, mensagem.Id,
                    mensagem.CorrelationId, mensagem.TraceParent, ct);
                mensagem.MarcarComoProcessado(relogio.AgoraUtc);
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Não deu pra publicar a mensagem {MensagemId} na fila {Fila}.", mensagem.Id, mensagem.Fila);
                mensagem.RegistrarFalha("Não deu pra publicar no RabbitMQ (detalhes no log da API).", relogio.AgoraUtc);
            }

            outbox.Atualizar(mensagem);
            await context.SaveChangesAsync(ct);
        }
    }
}
