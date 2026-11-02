using RestauranteEstoque.Core.Persistencia.Mensageria;
using RestauranteEstoque.Email.Infrastructure.Persistence;
using RestauranteEstoque.Core.Api;
using RestauranteEstoque.Email.Api.Workers;
using RestauranteEstoque.Email.Application;
using RestauranteEstoque.Email.Infrastructure;

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
