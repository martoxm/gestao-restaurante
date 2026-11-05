using GestaoRestaurante.Core.Persistencia.Mensageria;
using GestaoRestaurante.Core.Tempo;
using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using GestaoRestaurante.Core.Application.Abstractions;
using GestaoRestaurante.Core.Mensageria;
using GestaoRestaurante.Core.Seguranca;
using GestaoRestaurante.Pagamentos.Application.Abstractions.Repositories;
using GestaoRestaurante.Pagamentos.Application.Abstractions.Services;
using GestaoRestaurante.Pagamentos.Infrastructure.Assinaturas;
using GestaoRestaurante.Pagamentos.Infrastructure.Gateways;
using GestaoRestaurante.Pagamentos.Infrastructure.Persistence;
using GestaoRestaurante.Pagamentos.Infrastructure.Persistence.Repositories;
using Stripe;

namespace GestaoRestaurante.Pagamentos.Infrastructure;

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

        services.AddScoped<IPagamentoRepository, PagamentoRepository>();
        services.AddScoped<ICobrancaPlanoRepository, CobrancaPlanoRepository>();
        var planos = configuration.GetSection(PlanosSettings.SectionName).Get<PlanosSettings>() ?? new PlanosSettings();
        services.AddSingleton<ITabelaPlanos>(planos);
        services.AddSingleton(configuration.GetSection(FrontSettings.SectionName).Get<FrontSettings>() ?? new FrontSettings());
        services.AddOutboxMensagens<AppDbContext>();
        services.AddMensageriaRabbitMq(configuration);

        var gatewayFalso = configuration.GetSection("GatewayFalso").Get<GatewayFalsoSettings>() ?? new GatewayFalsoSettings();
        services.AddSingleton(gatewayFalso);
        var stripe = configuration.GetSection("Stripe").Get<StripeSettings>() ?? new StripeSettings();
        services.AddSingleton(stripe);

        var provedor = configuration["Gateway:Provedor"] ?? "Falso";
        if (provedor.Equals("Stripe", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(stripe.ChaveSecreta))
                throw new InvalidOperationException("Gateway:Provedor = Stripe, mas Stripe:ChaveSecreta está vazia (configure no user-secrets).");

            services.AddSingleton(new StripeClient(stripe.ChaveSecreta));
            services.AddSingleton<IGatewayPagamento, GatewayStripe>();
            services.AddSingleton<ICheckoutPlano, CheckoutPlanoStripe>();
        }
        else
        {
            services.AddSingleton<IGatewayPagamento, GatewayPagamentoFalso>();
            services.AddSingleton<ICheckoutPlano, CheckoutPlanoFalso>();

            services.AddSingleton(Channel.CreateUnbounded<CobrancaFalsa>());
            services.AddHttpClient(SimuladorWebhookGatewayFalso.NomeHttpClient);
            services.AddHostedService<SimuladorWebhookGatewayFalso>();
        }

        services.AddHttpContextAccessor();
        services.AddScoped<UsuarioAtualService>();
        services.AddScoped<IUsuarioAtualService>(provider => provider.GetRequiredService<UsuarioAtualService>());
        services.AddScoped<IDefinidorRestauranteAtual>(provider => provider.GetRequiredService<UsuarioAtualService>());

        services.AddRelogio();

        return services;
    }
}
