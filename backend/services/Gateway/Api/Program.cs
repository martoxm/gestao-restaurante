using System.Threading.RateLimiting;
using GestaoRestaurante.Core.Api;
using GestaoRestaurante.Core.Seguranca;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

const string FrontendCorsPolicy = "FrontendCorsPolicy";
var allowedOrigin = builder.Configuration["Cors:AllowedOrigin"]!;
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
        policy.WithOrigins(allowedOrigin)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .WithExposedHeaders(CorrelationIdExtensions.NomeCabecalho));
});

var geralPorMinuto = builder.Configuration.GetValue("LimiteRequisicoes:GeralPorMinuto", 300);
var autenticacaoPorMinuto = builder.Configuration.GetValue("LimiteRequisicoes:AutenticacaoPorMinuto", 20);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("geral", context => LimitePorIp(context, geralPorMinuto));
    options.AddPolicy("autenticacao", context => LimitePorIp(context, autenticacaoPorMinuto));
});

builder.Services.AddAutenticacaoJwt(builder.Configuration);

var app = builder.Build();

app.MapDefaultEndpoints();

app.Use(async (context, next) =>
{
    if (context.Request.Path.Value?.Contains("/api/v1/interno", StringComparison.OrdinalIgnoreCase) == true)
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    await next(context);
});

app.Use(async (context, next) =>
{
    if (string.IsNullOrWhiteSpace(context.Request.Headers[CorrelationIdExtensions.NomeCabecalho]))
        context.Request.Headers[CorrelationIdExtensions.NomeCabecalho] = Guid.NewGuid().ToString();

    await next(context);
});
app.UseCorrelationId();

app.UseCors(FrontendCorsPolicy);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => "API Gateway no ar!");

app.MapReverseProxy();

app.Run();

static RateLimitPartition<string> LimitePorIp(HttpContext context, int permitidasPorMinuto) =>
    RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = permitidasPorMinuto,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        });
