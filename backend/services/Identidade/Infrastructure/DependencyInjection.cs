using GestaoRestaurante.Core.Tempo;
using GestaoRestaurante.Core.Mensageria;
using GestaoRestaurante.Core.Persistencia.Mensageria;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using GestaoRestaurante.Core.Application.Abstractions;
using GestaoRestaurante.Core.Seguranca;
using GestaoRestaurante.Identidade.Application.Abstractions.Repositories;
using GestaoRestaurante.Identidade.Application.Abstractions.Services;
using GestaoRestaurante.Identidade.Infrastructure.Identity;
using GestaoRestaurante.Identidade.Infrastructure.Links;
using GestaoRestaurante.Identidade.Infrastructure.Plataforma;
using GestaoRestaurante.Identidade.Infrastructure.Persistence;
using GestaoRestaurante.Identidade.Infrastructure.Persistence.Repositories;
using GestaoRestaurante.Identidade.Infrastructure.Security;

namespace GestaoRestaurante.Identidade.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddHealthChecks().AddDbContextCheck<AppDbContext>("banco", tags: ["ready"]);

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString,
                    sqlServer => sqlServer
                        .MigrationsHistoryTable("__EFMigrationsHistory", "identidade")
                        .EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null))
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<AppDbContext>());

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireLowercase = false;
                options.Password.RequiredLength = 6;
            })
            .AddRoles<ApplicationRole>()
            .AddErrorDescriber<IdentityErrorDescriberPtBr>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IRestauranteRepository, RestauranteRepository>();

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

        services.AddSingleton<IGeradorRefreshToken, GeradorRefreshToken>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

        services.AddScoped<IConviteRepository, ConviteRepository>();
        services.AddScoped<IAuditoriaPlataformaRepository, AuditoriaPlataformaRepository>();
        services.AddScoped<INotificacaoPlataformaRepository, NotificacaoPlataformaRepository>();

        services.AddScoped<IConsultasPlataforma, ConsultasPlataforma>();
        services.Configure<PlanosSettings>(configuration.GetSection(PlanosSettings.SectionName));
        services.AddOutboxMensagens<AppDbContext>();
        services.AddIdempotenciaMensagens<AppDbContext>();
        services.AddMensageriaRabbitMq(configuration);
        services.Configure<FrontSettings>(configuration.GetSection(FrontSettings.SectionName));
        services.AddSingleton<ILinksFront, LinksFront>();

        services.AddHttpContextAccessor();
        services.AddScoped<IUsuarioAtualService, UsuarioAtualService>();

        services.AddRelogio();

        return services;
    }
}
