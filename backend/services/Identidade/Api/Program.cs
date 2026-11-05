using System.Text.Json.Serialization;
using Asp.Versioning;
using Microsoft.AspNetCore.Identity;
using Microsoft.OpenApi;
using GestaoRestaurante.Core.Api;
using GestaoRestaurante.Core.Persistencia.Mensageria;
using GestaoRestaurante.Core.Seguranca;
using GestaoRestaurante.Identidade.Api.Modules;
using GestaoRestaurante.Identidade.Api.Workers;
using GestaoRestaurante.Identidade.Application;
using GestaoRestaurante.Identidade.Infrastructure;
using GestaoRestaurante.Identidade.Infrastructure.Identity;
using GestaoRestaurante.Identidade.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddEndpointsApiExplorer();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Informe o token assim: Bearer {seu token}"
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document, null)] = []
    });
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
})
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});


builder.Services.AddAutenticacaoJwt(builder.Configuration);

builder.Services.AddHostedService<LimpezaRefreshTokensWorker>();
builder.Services.AddHostedService<AvisoFimDoTesteWorker>();
builder.Services.AddHostedService<PlanoPagoConsumidor>();

builder.Services.AddHostedService<PublicacaoOutboxWorker<AppDbContext>>();
builder.Services.AddHostedService<LimpezaMensageriaWorker<AppDbContext>>();

var app = builder.Build();

app.MapDefaultEndpoints();

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
    string[] roleNames = [RoleNames.Admin, RoleNames.Funcionario, RoleNames.SuperAdmin];

    foreach (var roleName in roleNames)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new ApplicationRole { Name = roleName });
        }
    }

    await SemeadorSuperAdmin.SemearAsync(
        scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(),
        app.Configuration,
        app.Logger);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCorrelationId();

app.UseValidationExceptionHandling();

app.UseAuthentication();
app.UseBloqueioRestauranteNaoLiberado();
app.UseAuthorization();

app.MapGet("/", () => "API de Identidade no ar!");

IEndpointModule[] endpointModules =
[
    new AuthEndpointsModule(),
    new RestauranteEndpointsModule(),
    new InternoEndpointsModule(),
    new UsuarioEndpointsModule(),
    new ConviteEndpointsModule(),
    new PlataformaEndpointsModule()
];

foreach (var module in endpointModules)
{
    module.MapEndpoints(app);
}

app.Run();
