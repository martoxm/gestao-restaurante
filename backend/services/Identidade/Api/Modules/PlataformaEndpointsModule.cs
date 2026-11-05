using System.Security.Claims;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using GestaoRestaurante.Core.Api;
using GestaoRestaurante.Core.Application.Exceptions;
using GestaoRestaurante.Core.Application.Models;
using GestaoRestaurante.Core.Seguranca;
using GestaoRestaurante.Identidade.Application.UseCases.Plataforma;
using GestaoRestaurante.Identidade.Contracts.Convites;
using GestaoRestaurante.Identidade.Contracts.Plataforma;

namespace GestaoRestaurante.Identidade.Api.Modules;

public class PlataformaEndpointsModule : IEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var versionSet = app.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();

        var group = app.MapGroup("/api/v{version:apiVersion}/plataforma")
            .WithApiVersionSet(versionSet)
            .WithTags("Plataforma")
            .RequireAuthorization(policy => policy.RequireRole(RoleNames.SuperAdmin));

        group.MapGet("/resumo", ObterResumo)
            .WithName("ObterResumoPlataforma")
            .WithSummary("Números gerais da plataforma")
            .WithDescription("Restaurantes por plano e situação, usuários, convites, funil de conversão, MRR estimado e cadastros por dia (últimos 30). Exige a role SuperAdmin.")
            .Produces<ResumoPlataformaDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status403Forbidden);

        group.MapGet("/restaurantes", ListarRestaurantes)
            .WithName("ListarRestaurantesPlataforma")
            .WithSummary("Lista todos os restaurantes")
            .WithDescription("Busca pelo nome ou pelo e-mail de alguém do restaurante; filtros por plano (Teste/Basico/Pro) e status (Ativo, Inativo, Pago, EmTeste, VencendoEmBreve, TesteVencido). Do mais novo pro mais antigo. Exige a role SuperAdmin.")
            .Produces<ResultadoPaginado<RestaurantePlataformaItemDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden);

        group.MapGet("/restaurantes/{id:guid}", ObterRestaurante)
            .WithName("ObterRestaurantePlataforma")
            .WithSummary("Detalhe de um restaurante")
            .WithDescription("Plano, prazo do teste, usuários (com role e se estão ativos) e convites. Exige a role SuperAdmin.")
            .Produces<RestaurantePlataformaDetalhesDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPut("/restaurantes/{id:guid}/plano", MudarPlano)
            .WithName("MudarPlanoRestaurante")
            .WithSummary("Troca o plano do restaurante")
            .WithDescription("Plano pago apaga o prazo do teste; voltar pro Teste dá 14 dias a partir de agora. Vale pro token na próxima renovação (até 15 min). Exige a role SuperAdmin.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/restaurantes/{id:guid}/estender-teste", EstenderTeste)
            .WithName("EstenderTesteRestaurante")
            .WithSummary("Estende o período de teste")
            .WithDescription("De 1 a 90 dias. Se o teste já venceu, conta a partir de agora. Só no plano Teste. Exige a role SuperAdmin.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPut("/restaurantes/{id:guid}/ativo", AlterarAtivoRestaurante)
            .WithName("AlterarAtivoRestaurante")
            .WithSummary("Ativa ou desativa o restaurante")
            .WithDescription("Desativado continua entrando e lendo, mas não grava nada (402). Os dados não somem. Exige a role SuperAdmin.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/convites/{id:guid}/reenviar", ReenviarConvite)
            .WithName("ReenviarConvitePlataforma")
            .WithSummary("Reenvia um convite")
            .WithDescription("Cancela o convite e manda um link novo por e-mail (mais 7 dias). Retorna 400 se o convite já foi aceito. Exige a role SuperAdmin.")
            .Produces<ConviteCriadoResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/usuarios/{id:guid}/forcar-logout", ForcarLogout)
            .WithName("ForcarLogoutUsuario")
            .WithSummary("Derruba as sessões de um usuário")
            .WithDescription("Apaga os refresh tokens: a pessoa cai no login quando o token de acesso vencer (até 15 min). Exige a role SuperAdmin.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPut("/usuarios/{id:guid}/ativo", AlterarAtivoUsuario)
            .WithName("AlterarAtivoUsuarioPlataforma")
            .WithSummary("Ativa ou desativa um usuário de qualquer restaurante")
            .WithDescription("Não deixa o restaurante sem nenhum Admin ativo. Exige a role SuperAdmin.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/auditoria", ListarAuditoria)
            .WithName("ListarAuditoriaPlataforma")
            .WithSummary("Histórico das ações do SuperAdmin")
            .WithDescription("Da mais nova pra mais antiga; dá pra filtrar por restaurante. Exige a role SuperAdmin.")
            .Produces<ResultadoPaginado<AuditoriaItemDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> ObterResumo(ISender sender, CancellationToken cancellationToken) =>
        Results.Ok(await sender.Send(new ObterResumoPlataformaQuery(), cancellationToken));

    private static async Task<IResult> ListarRestaurantes([AsParameters] ListarRestaurantesPlataformaRequest filtro, ISender sender, CancellationToken cancellationToken) =>
        Results.Ok(await sender.Send(new ListarRestaurantesPlataformaQuery(filtro), cancellationToken));

    private static async Task<IResult> ObterRestaurante(Guid id, ISender sender, CancellationToken cancellationToken) =>
        Results.Ok(await sender.Send(new ObterRestaurantePlataformaQuery(id), cancellationToken));

    private static async Task<IResult> MudarPlano(Guid id, MudarPlanoRequest request, ClaimsPrincipal user, ISender sender, CancellationToken cancellationToken)
    {
        await sender.Send(new MudarPlanoRestauranteCommand(QuemFaz(user), id, request.Plano), cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> EstenderTeste(Guid id, EstenderTesteRequest request, ClaimsPrincipal user, ISender sender, CancellationToken cancellationToken)
    {
        await sender.Send(new EstenderTesteRestauranteCommand(QuemFaz(user), id, request.Dias), cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> AlterarAtivoRestaurante(Guid id, AlterarAtivoRequest request, ClaimsPrincipal user, ISender sender, CancellationToken cancellationToken)
    {
        await sender.Send(new AlterarAtivoRestauranteCommand(QuemFaz(user), id, request.Ativo), cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> ReenviarConvite(Guid id, ClaimsPrincipal user, ISender sender, CancellationToken cancellationToken) =>
        Results.Ok(await sender.Send(new ReenviarConviteCommand(QuemFaz(user), id), cancellationToken));

    private static async Task<IResult> ForcarLogout(Guid id, ClaimsPrincipal user, ISender sender, CancellationToken cancellationToken)
    {
        await sender.Send(new ForcarLogoutUsuarioCommand(QuemFaz(user), id), cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> AlterarAtivoUsuario(Guid id, AlterarAtivoRequest request, ClaimsPrincipal user, ISender sender, CancellationToken cancellationToken)
    {
        await sender.Send(new AlterarAtivoUsuarioPlataformaCommand(QuemFaz(user), id, request.Ativo), cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> ListarAuditoria([AsParameters] ListarAuditoriaRequest filtro, ISender sender, CancellationToken cancellationToken) =>
        Results.Ok(await sender.Send(new ListarAuditoriaQuery(filtro), cancellationToken));

    private static SuperAdminAtual QuemFaz(ClaimsPrincipal user)
    {
        var id = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        var email = user.FindFirstValue(ClaimTypes.Email) ?? user.FindFirstValue("email");

        if (!Guid.TryParse(id, out var usuarioId) || string.IsNullOrWhiteSpace(email))
            throw new SessaoInvalidaException("Token sem a identificação do usuário. Entre de novo.");

        return new SuperAdminAtual(usuarioId, email);
    }
}
