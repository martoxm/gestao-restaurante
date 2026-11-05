using System.Security.Claims;
using GestaoRestaurante.Core.Api;
using Asp.Versioning;
using MediatR;
using GestaoRestaurante.Identidade.Application.UseCases.Auth;
using GestaoRestaurante.Identidade.Application.UseCases.Convites;
using GestaoRestaurante.Identidade.Contracts.Convites;
using GestaoRestaurante.Identidade.Contracts.Auth;

namespace GestaoRestaurante.Identidade.Api.Modules;

public class AuthEndpointsModule : IEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var versionSet = app.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();

        var group = app.MapGroup("/api/v{version:apiVersion}/auth")
            .WithApiVersionSet(versionSet)
            .WithTags("Auth");

        group.MapPost("/login", Login)
            .WithName("Login")
            .WithSummary("Autentica um usuário")
            .WithDescription("Valida e-mail e senha e devolve um token JWT, que deve ser enviado no cabeçalho \"Authorization: Bearer {token}\" dos demais endpoints protegidos.")
            .Produces<LoginResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/registrar", Registrar)
            .WithName("Registrar")
            .WithSummary("Cadastra um novo usuário")
            .WithDescription("Cria um usuário novo (nome, e-mail e senha) e já devolve um token JWT, exatamente como o /login — não é preciso logar de novo depois de se cadastrar.")
            .Produces<LoginResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapPost("/renovar", Renovar)
            .WithName("RenovarToken")
            .WithSummary("Renova a sessão")
            .WithDescription("Troca um refresh token válido por um token de acesso novo (15 min) e um refresh token novo (14 dias). O refresh token enviado é revogado na hora: cada um só serve uma vez.")
            .Produces<LoginResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapGet("/convite", ObterConvite)
            .WithName("ObterConvitePorToken")
            .WithSummary("Mostra um convite pelo token do link")
            .WithDescription("Devolve o e-mail, o restaurante e a role do convite. Retorna 404 se o token não existir, já tiver sido usado, cancelado ou vencido.")
            .Produces<ConvitePublicoDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/aceitar-convite", AceitarConvite)
            .WithName("AceitarConvite")
            .WithSummary("Aceita um convite e cria a conta")
            .WithDescription("Cria o usuário no restaurante e com a role do convite (nunca do corpo da requisição) e já devolve o token, igual ao /registrar. O convite só pode ser usado uma vez.")
            .Produces<LoginResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/esqueci-senha", EsqueciSenha)
            .WithName("EsqueciSenha")
            .WithSummary("Pede um link para redefinir a senha")
            .WithDescription("Se o e-mail tiver conta ativa, manda um link (vale 1 dia, uso único). Responde 204 sempre, exista o e-mail ou não.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapPost("/redefinir-senha", RedefinirSenha)
            .WithName("RedefinirSenha")
            .WithSummary("Define a senha nova pelo link do e-mail")
            .WithDescription("Troca a senha e encerra todas as sessões abertas do usuário. Retorna 400 se o link for inválido/vencido ou a senha não atender a política.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapPost("/confirmar-email", ConfirmarEmail)
            .WithName("ConfirmarEmail")
            .WithSummary("Confirma o e-mail pelo link")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapPost("/reenviar-confirmacao", ReenviarConfirmacao)
            .RequireAuthorization()
            .WithName("ReenviarConfirmacaoEmail")
            .WithSummary("Manda de novo o e-mail de confirmação")
            .WithDescription("Usa o usuário do token. Retorna 400 se o e-mail já estiver confirmado.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> EsqueciSenha(EsqueciSenhaCommand command, ISender sender, CancellationToken cancellationToken)
    {
        await sender.Send(command, cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> RedefinirSenha(RedefinirSenhaCommand command, ISender sender, CancellationToken cancellationToken)
    {
        await sender.Send(command, cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> ConfirmarEmail(ConfirmarEmailCommand command, ISender sender, CancellationToken cancellationToken)
    {
        await sender.Send(command, cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> ReenviarConfirmacao(ClaimsPrincipal user, ISender sender, CancellationToken cancellationToken)
    {
        var id = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        if (!Guid.TryParse(id, out var usuarioId))
            return Results.Unauthorized();

        await sender.Send(new ReenviarConfirmacaoEmailCommand(usuarioId), cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> ObterConvite(string token, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ObterConvitePorTokenQuery(token), cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> AceitarConvite(AceitarConviteCommand command, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> Renovar(RenovarTokenCommand command, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> Login(LoginCommand command, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> Registrar(RegistrarCommand command, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return Results.Ok(result);
    }
}
