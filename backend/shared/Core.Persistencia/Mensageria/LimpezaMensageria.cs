using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using GestaoRestaurante.Core.Application.Abstractions;
using GestaoRestaurante.Core.Mensageria;

namespace GestaoRestaurante.Core.Persistencia.Mensageria;

public static class LimpezaMensageria
{
    public static readonly TimeSpan Retencao = TimeSpan.FromDays(7);

    public static async Task<(int Outbox, int Processadas)> LimparAsync(DbContext context, DateTime agora, CancellationToken ct)
    {
        var limite = agora - Retencao;
        var outbox = 0;
        var processadas = 0;

        if (context.Model.FindEntityType(typeof(OutboxMensagem)) is not null)
        {
            outbox = await context.Set<OutboxMensagem>()
                .Where(mensagem => mensagem.ProcessadoEm != null && mensagem.ProcessadoEm < limite)
                .ExecuteDeleteAsync(ct);
        }

        if (context.Model.FindEntityType(typeof(MensagemProcessada)) is not null)
        {
            processadas = await context.Set<MensagemProcessada>()
                .Where(mensagem => mensagem.ProcessadoEm < limite)
                .ExecuteDeleteAsync(ct);
        }

        return (outbox, processadas);
    }
}

public sealed class LimpezaMensageriaWorker<TContext> : BackgroundService
    where TContext : DbContext
{
    private static readonly TimeSpan Intervalo = TimeSpan.FromHours(12);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LimpezaMensageriaWorker<TContext>> _logger;

    public LimpezaMensageriaWorker(IServiceScopeFactory scopeFactory, ILogger<LimpezaMensageriaWorker<TContext>> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Intervalo);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<TContext>();
                var relogio = scope.ServiceProvider.GetRequiredService<IRelogio>();

                var (outbox, processadas) = await LimpezaMensageria.LimparAsync(context, relogio.AgoraUtc, stoppingToken);
                _logger.LogInformation("Limpeza da mensageria: {Outbox} do outbox e {Processadas} mensagens processadas removidas.", outbox, processadas);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Falha na limpeza da mensageria.");
            }
        }
    }
}
