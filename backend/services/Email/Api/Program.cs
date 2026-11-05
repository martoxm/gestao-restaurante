using GestaoRestaurante.Core.Persistencia.Mensageria;
using GestaoRestaurante.Email.Infrastructure.Persistence;
using GestaoRestaurante.Core.Api;
using GestaoRestaurante.Email.Api.Workers;
using GestaoRestaurante.Email.Application;
using GestaoRestaurante.Email.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHostedService<EnvioEmailConsumidor>();
builder.Services.AddHostedService<LimpezaMensageriaWorker<AppDbContext>>();

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseCorrelationId();

app.MapGet("/", () => "API de Email no ar!");

app.Run();
