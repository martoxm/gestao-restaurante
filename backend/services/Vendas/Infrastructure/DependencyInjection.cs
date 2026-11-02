using RestauranteEstoque.Core.Persistencia.Mensageria;
using RestauranteEstoque.Core.Tempo;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RestauranteEstoque.Core.Application.Abstractions;
using RestauranteEstoque.Core.Mensageria;
using RestauranteEstoque.Core.Seguranca;
using RestauranteEstoque.Vendas.Application.Abstractions.Repositories;
using RestauranteEstoque.Vendas.Application.Abstractions.Services;
using RestauranteEstoque.Vendas.Infrastructure.Persistence;
using RestauranteEstoque.Vendas.Infrastructure.Persistence.Repositories;

namespace RestauranteEstoque.Vendas.Infrastructure;

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

        services.AddScoped<IVendaRepository, VendaRepository>();
        services.AddOutboxMensagens<AppDbContext>();
        services.AddIdempotenciaMensagens<AppDbContext>();
        services.AddScoped<IProdutoCatalogoRepository, ProdutoCatalogoRepository>();

        services.AddMensageriaRabbitMq(configuration);

        services.AddHttpContextAccessor();
        services.AddScoped<UsuarioAtualService>();
        services.AddScoped<IUsuarioAtualService>(provider => provider.GetRequiredService<UsuarioAtualService>());
        services.AddScoped<IDefinidorRestauranteAtual>(provider => provider.GetRequiredService<UsuarioAtualService>());

        services.AddRelogio();

        return services;
    }
}
