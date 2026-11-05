using MediatR;
using GestaoRestaurante.Core.Application.Models;
using GestaoRestaurante.Contracts.Common;
using GestaoRestaurante.Contracts.Fornecedores;

namespace GestaoRestaurante.Application.UseCases.Fornecedores;

public record ObterFornecedorPorIdQuery(Guid Id) : IRequest<FornecedorDetalhesDto?>;

public record ListarFornecedoresQuery(
    int Pagina = 1,
    int TamanhoPagina = 10,
    string? Nome = null,
    DirecaoOrdenacao DirecaoOrdenacao = DirecaoOrdenacao.Asc) : IRequest<ResultadoPaginado<FornecedorItemListaDto>>;
