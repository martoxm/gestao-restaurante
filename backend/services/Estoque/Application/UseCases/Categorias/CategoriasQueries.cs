using MediatR;
using GestaoRestaurante.Core.Application.Models;
using GestaoRestaurante.Contracts.Categorias;
using GestaoRestaurante.Contracts.Common;

namespace GestaoRestaurante.Application.UseCases.Categorias;

public record ListarCategoriasQuery(
    int Pagina = 1,
    int TamanhoPagina = 10,
    string? Nome = null,
    DirecaoOrdenacao DirecaoOrdenacao = DirecaoOrdenacao.Asc) : IRequest<ResultadoPaginado<CategoriaItemListaDto>>;

public record ObterCategoriaPorIdQuery(Guid Id) : IRequest<CategoriaDetalhesDto?>;
