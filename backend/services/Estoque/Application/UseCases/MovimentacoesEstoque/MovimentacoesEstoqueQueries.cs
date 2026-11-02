using MediatR;
using RestauranteEstoque.Contracts.MovimentacoesEstoque;
using RestauranteEstoque.Core.Application.Models;

namespace RestauranteEstoque.Application.UseCases.MovimentacoesEstoque;

public record ListarMovimentacoesEstoqueQuery(
    int Pagina = 1,
    int TamanhoPagina = 10,
    Guid? ProdutoId = null,
    string? Tipo = null,
    DateTime? DataInicio = null,
    DateTime? DataFim = null) : IRequest<ResultadoPaginado<MovimentacaoEstoqueItemListaDto>>;
