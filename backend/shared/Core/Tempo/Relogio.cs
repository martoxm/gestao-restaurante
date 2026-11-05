using Microsoft.Extensions.DependencyInjection;
using GestaoRestaurante.Core.Application.Abstractions;

namespace GestaoRestaurante.Core.Tempo;

public class Relogio : IRelogio
{
    private readonly TimeProvider _timeProvider;

    public Relogio(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public DateTime AgoraUtc => _timeProvider.GetUtcNow().UtcDateTime;
}

public static class RelogioExtensions
{
    public static IServiceCollection AddRelogio(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IRelogio, Relogio>();
        return services;
    }
}
