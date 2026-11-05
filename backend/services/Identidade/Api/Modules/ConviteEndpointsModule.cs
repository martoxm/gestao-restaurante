using Asp.Versioning;
using MediatR;
using GestaoRestaurante.Core.Api;
using GestaoRestaurante.Core.Seguranca;
using GestaoRestaurante.Identidade.Application.UseCases.Convites;
using GestaoRestaurante.Identidade.Contracts.Convites;

namespace GestaoRestaurante.Identidade.Api.Modules;

public class ConviteEndpointsModule : IEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var versionSet = app.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();

        var group = app.MapGroup("/api/v{version:apiVersion}/convites")
            .WithApiVersionSet(versionSet)
            .WithTags("Convites")
            .RequireAuthorization(policy => policy.RequireRole(RoleNames.Admin));

        group.MapPost("", Criar)
            .WithName("CriarConvite")
            .WithSummary("Convida alguém para o restaurante")
            .WithDescription("Cria um convite (válido por 7 dias, uso único) e manda o link por e-mail. Convidar de novo o mesmo e-mail cancela o convite anterior. Retorna 400 se o e-mail já tiver conta. Exige a role Admin.")
            .Produces<ConviteCriadoResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden);

        group.MapGet("", Listar)
            .WithName("ListarConvites")
            .WithSummary("Lista os convites do restaurante")
            .WithDescription("Do mais novo pro mais antigo, com o status calculado na hora (Pendente, Aceito, Cancelado ou Expirado). Exige a role Admin.")
            .Produces<IReadOnlyList<ConviteItemListaDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status403Forbidden);

        group.MapDelete("/{id:guid}", Cancelar)
            .WithName("CancelarConvite")
            .WithSummary("Cancela um convite")
            .WithDescription("O link para de funcionar na hora. Retorna 400 se o convite já foi aceito e 404 se for de outro restaurante. Exige a role Admin.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Criar(CriarConviteRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CriarConviteCommand(request.Email, request.Role), cancellationToken);
        return Results.Created($"/api/v1/convites/{result.Id}", result);
    }

    private static async Task<IResult> Listar(ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListarConvitesQuery(), cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> Cancelar(Guid id, ISender sender, CancellationToken cancellationToken)
    {
        await sender.Send(new CancelarConviteCommand(id), cancellationToken);
        return Results.NoContent();
    }
}
