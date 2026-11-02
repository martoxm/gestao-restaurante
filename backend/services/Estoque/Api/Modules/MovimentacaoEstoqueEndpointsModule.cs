using RestauranteEstoque.Core.Application.Models;
using RestauranteEstoque.Core.Api;
using Asp.Versioning;
using MediatR;
using RestauranteEstoque.Application.UseCases.MovimentacoesEstoque;
using RestauranteEstoque.Contracts.MovimentacoesEstoque;

namespace RestauranteEstoque.Api.Modules;

public class MovimentacaoEstoqueEndpointsModule : IEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var versionSet = app.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();

        var group = app.MapGroup("/api/v{version:apiVersion}/movimentacoes-estoque")
            .WithApiVersionSet(versionSet)
            .WithTags("MovimentacoesEstoque")
            .RequireAuthorization();

        group.MapPost("", CriarMovimentacaoEstoque)
            .WithName("CriarMovimentacaoEstoque")
            .WithSummary("Registra uma movimentação de estoque")
            .WithDescription("Registra uma entrada ou saída de estoque para um produto existente, atualizando a quantidade em estoque dele. Retorna 404 se o produto não existir, e 400 se a saída pedida for maior que o estoque disponível.")
            .Produces<MovimentacaoEstoqueCriadaResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("", ListarMovimentacoesEstoque)
            .WithName("ListarMovimentacoesEstoque")
            .WithSummary("Lista o histórico de movimentações com paginação")
            .WithDescription("Lista as movimentações de estoque do restaurante, da mais nova pra mais antiga, com filtros opcionais por produto, tipo (Entrada/Saida) e período.")
            .Produces<ResultadoPaginado<MovimentacaoEstoqueItemListaDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> ListarMovimentacoesEstoque([AsParameters] ListarMovimentacoesEstoqueRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var query = new ListarMovimentacoesEstoqueQuery(request.Pagina, request.TamanhoPagina, request.ProdutoId, request.Tipo, request.DataInicio, request.DataFim);
        var result = await sender.Send(query, cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> CriarMovimentacaoEstoque(CriarMovimentacaoEstoqueCommand command, ISender sender, CancellationToken cancellationToken)
    {
        var id = await sender.Send(command, cancellationToken);
        return Results.Created($"/api/v1/movimentacoes-estoque/{id}", new MovimentacaoEstoqueCriadaResponse(id));
    }
}
