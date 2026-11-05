using Asp.Versioning;
using MediatR;
using GestaoRestaurante.Core.Api;
using GestaoRestaurante.Core.Seguranca;
using GestaoRestaurante.Identidade.Application.UseCases.Usuarios;
using GestaoRestaurante.Identidade.Contracts.Usuarios;

namespace GestaoRestaurante.Identidade.Api.Modules;

public class UsuarioEndpointsModule : IEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var versionSet = app.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();

        var group = app.MapGroup("/api/v{version:apiVersion}/usuarios")
            .WithApiVersionSet(versionSet)
            .WithTags("Usuarios")
            .RequireAuthorization(policy => policy.RequireRole(RoleNames.Admin));

        group.MapGet("", ListarUsuarios)
            .WithName("ListarUsuarios")
            .WithSummary("Lista os usuários do restaurante")
            .WithDescription("Lista os usuários do restaurante do token, com a role de cada um. Exige a role Admin.")
            .Produces<IReadOnlyList<UsuarioItemListaDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status403Forbidden);

        group.MapPut("/{id:guid}/role", AlterarRole)
            .WithName("AlterarRoleUsuario")
            .WithSummary("Troca a role de um usuário")
            .WithDescription("Troca entre Admin e Funcionario. Retorna 400 se o usuário for o último Admin do restaurante e 404 se for de outro restaurante. A pessoa só vê a role nova quando o token dela for renovado.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}/ativo", AlterarAtivo)
            .WithName("AlterarAtivoUsuario")
            .WithSummary("Desativa ou reativa um usuário")
            .WithDescription("Desativado não consegue logar nem renovar a sessão (as sessões abertas caem em até 15 min) e não ocupa vaga do plano. Admin precisa ser rebaixado antes. Reativar respeita o limite do plano. Exige a role Admin.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> AlterarAtivo(Guid id, AlterarAtivoUsuarioRequest request, ISender sender, CancellationToken cancellationToken)
    {
        await sender.Send(new AlterarAtivoUsuarioCommand(id, request.Ativo), cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> ListarUsuarios(ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListarUsuariosQuery(), cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> AlterarRole(Guid id, AlterarRoleUsuarioRequest request, ISender sender, CancellationToken cancellationToken)
    {
        await sender.Send(new AlterarRoleUsuarioCommand(id, request.Role), cancellationToken);
        return Results.NoContent();
    }
}
