using MediatR;
using GestaoRestaurante.Contracts.MovimentacoesEstoque;
using GestaoRestaurante.Core.Application.Models;

namespace GestaoRestaurante.Application.UseCases.MovimentacoesEstoque;

public record ListarMovimentacoesEstoqueQuery(
    int Pagina = 1,
    int TamanhoPagina = 10,
    Guid? ProdutoId = null,
    string? Tipo = null,
    DateTime? DataInicio = null,
    DateTime? DataFim = null) : IRequest<ResultadoPaginado<MovimentacaoEstoqueItemListaDto>>;
