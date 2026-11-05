using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using GestaoRestaurante.Core.Mensageria;

namespace GestaoRestaurante.Core.Persistencia.Mensageria;

public static class MensageriaPersistenciaExtensions
{
    public static IServiceCollection AddOutboxMensagens<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        services.AddScoped<IOutboxMensagemRepository, OutboxMensagemRepository<TContext>>();
        return services;
    }

    public static IServiceCollection AddIdempotenciaMensagens<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        services.AddScoped<IMensagemProcessadaRepository, MensagemProcessadaRepository<TContext>>();
        return services;
    }
}
