using Asp.Versioning;
using MediatR;
using GestaoRestaurante.Core.Api;
using GestaoRestaurante.Core.Seguranca;
using GestaoRestaurante.Pagamentos.Application.UseCases.Assinaturas;
using GestaoRestaurante.Pagamentos.Contracts.Assinaturas;

namespace GestaoRestaurante.Pagamentos.Api.Modules;

public class AssinaturaEndpointsModule : IEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var versionSet = app.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();

        var group = app.MapGroup("/api/v{version:apiVersion}/assinaturas")
            .WithApiVersionSet(versionSet)
            .WithTags("Assinaturas")
            .RequireAuthorization(policy => policy.RequireRole(RoleNames.Admin));

        group.MapGet("/planos", ListarPlanos)
            .WithName("ListarPlanos")
            .WithSummary("Lista os planos à venda")
            .WithDescription("Preço e quantos dias cada pagamento vale. O preço vem do servidor (seção Planos do appsettings).")
            .Produces<IReadOnlyList<PlanoAVendaDto>>(StatusCodes.Status200OK);

        group.MapPost("/checkout", IniciarAssinatura)
            .WithName("IniciarAssinatura")
            .WithSummary("Começa o pagamento de um plano")
            .WithDescription("Cria a cobrança e a página de pagamento (Stripe Checkout em modo teste, ou simulada no gateway falso). O front redireciona pra urlPagamento.")
            .Produces<CheckoutAssinaturaResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem();
    }

    private static async Task<IResult> ListarPlanos(ISender sender, CancellationToken cancellationToken) =>
        Results.Ok(await sender.Send(new ListarPlanosQuery(), cancellationToken));

    private static async Task<IResult> IniciarAssinatura(IniciarAssinaturaRequest request, ISender sender, CancellationToken cancellationToken) =>
        Results.Ok(await sender.Send(new IniciarAssinaturaCommand(request.Plano), cancellationToken));
}
