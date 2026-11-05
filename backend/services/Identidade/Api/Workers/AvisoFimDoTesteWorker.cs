using MediatR;
using GestaoRestaurante.Identidade.Application.UseCases.Plataforma;
using GestaoRestaurante.Identidade.Application.UseCases.Restaurantes;

namespace GestaoRestaurante.Identidade.Api.Workers;

public class AvisoFimDoTesteWorker : BackgroundService
{
    private static readonly TimeSpan PrimeiraPassada = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan Intervalo = TimeSpan.FromHours(12);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AvisoFimDoTesteWorker> _logger;

    public AvisoFimDoTesteWorker(IServiceScopeFactory scopeFactory, ILogger<AvisoFimDoTesteWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(PrimeiraPassada, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        using var timer = new PeriodicTimer(Intervalo);
        do
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var sender = scope.ServiceProvider.GetRequiredService<ISender>();

                var avisados = await sender.Send(new AvisarFimDoTesteCommand(), stoppingToken);
                _logger.LogInformation("Aviso de fim do teste: {Avisados} restaurante(s) avisado(s).", avisados);

                var vencidos = await sender.Send(new NotificarVencimentosCommand(), stoppingToken);
                _logger.LogInformation("Notificações de vencimento: {Vencidos} nova(s).", vencidos);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Falha no aviso de fim do teste.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
