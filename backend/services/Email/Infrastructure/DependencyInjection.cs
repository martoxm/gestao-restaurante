using GestaoRestaurante.Core.Persistencia.Mensageria;
using GestaoRestaurante.Core.Tempo;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using GestaoRestaurante.Core.Mensageria;
using GestaoRestaurante.Email.Application.Abstractions.Services;
using GestaoRestaurante.Email.Infrastructure.EnvioEmail;
using GestaoRestaurante.Email.Infrastructure.Persistence;

namespace GestaoRestaurante.Email.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddHealthChecks().AddDbContextCheck<AppDbContext>("banco", tags: ["ready"]);

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString, sqlServer => sqlServer.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null))
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<AppDbContext>());
        services.AddIdempotenciaMensagens<AppDbContext>();

        services.AddMensageriaRabbitMq(configuration);

        services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));
        services.AddScoped<IEmailService, SmtpEmailService>();

        services.AddRelogio();

        return services;
    }
}
