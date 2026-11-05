using GestaoRestaurante.Infrastructure.Persistence;
using GestaoRestaurante.Core.Persistencia.Mensageria;
using System.Text.Json.Serialization;
using Asp.Versioning;
using Microsoft.OpenApi;
using GestaoRestaurante.Core.Api;
using GestaoRestaurante.Core.Seguranca;
using GestaoRestaurante.Api.Modules;
using GestaoRestaurante.Api.Workers;
using GestaoRestaurante.Application;
using GestaoRestaurante.Infrastructure;

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

builder.Services.AddHostedService<PublicacaoOutboxWorker<AppDbContext>>();
builder.Services.AddHostedService<LimpezaMensageriaWorker<AppDbContext>>();
builder.Services.AddHostedService<AvisoAdminsConsumidor>();
builder.Services.AddHostedService<BaixaEstoqueConsumidor>();
builder.Services.AddHostedService<DevolucaoEstoqueConsumidor>();

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

var app = builder.Build();

app.MapDefaultEndpoints();

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

app.MapGet("/", () => "API Controle de Estoque de Restaurante no ar!");

IEndpointModule[] endpointModules =
[
    new ProdutoEndpointsModule(),
    new CategoriaEndpointsModule(),
    new FornecedorEndpointsModule(),
    new MovimentacaoEstoqueEndpointsModule()
];

foreach (var module in endpointModules)
{
    module.MapEndpoints(app);
}

app.Run();
