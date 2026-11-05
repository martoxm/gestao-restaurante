using MediatR;
using GestaoRestaurante.Core.Application.Models;
using GestaoRestaurante.Identidade.Contracts.Plataforma;

namespace GestaoRestaurante.Identidade.Application.UseCases.Plataforma;

public record ObterResumoPlataformaQuery : IRequest<ResumoPlataformaDto>;

public record ListarRestaurantesPlataformaQuery(ListarRestaurantesPlataformaRequest Filtro)
    : IRequest<ResultadoPaginado<RestaurantePlataformaItemDto>>;

public record ObterRestaurantePlataformaQuery(Guid Id) : IRequest<RestaurantePlataformaDetalhesDto>;

public record ListarAuditoriaQuery(ListarAuditoriaRequest Filtro) : IRequest<ResultadoPaginado<AuditoriaItemDto>>;
