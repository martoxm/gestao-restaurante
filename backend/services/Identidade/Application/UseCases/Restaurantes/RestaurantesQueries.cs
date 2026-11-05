using MediatR;
using GestaoRestaurante.Core.Application.Models;
using GestaoRestaurante.Identidade.Contracts.Common;
using GestaoRestaurante.Identidade.Contracts.Restaurantes;

namespace GestaoRestaurante.Identidade.Application.UseCases.Restaurantes;

public record ObterRestaurantePorIdQuery(Guid Id) : IRequest<RestauranteDetalhesDto?>;

public record ListarRestaurantesQuery(
    int Pagina = 1,
    int TamanhoPagina = 10,
    string? Nome = null,
    DirecaoOrdenacao DirecaoOrdenacao = DirecaoOrdenacao.Asc) : IRequest<ResultadoPaginado<RestauranteItemListaDto>>;

public record ListarEmailsAdminsQuery(Guid RestauranteId) : IRequest<EmailsAdminsDto>;
