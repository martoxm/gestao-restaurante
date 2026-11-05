using GestaoRestaurante.Core.Application.Models;
using GestaoRestaurante.Identidade.Contracts.Plataforma;

namespace GestaoRestaurante.Identidade.Application.Abstractions.Services;

public interface IConsultasPlataforma
{
    Task<ResumoPlataformaDto> ObterResumoAsync(DateTime agora, CancellationToken cancellationToken);

    Task<ResultadoPaginado<RestaurantePlataformaItemDto>> ListarRestaurantesAsync(
        ListarRestaurantesPlataformaRequest filtro, DateTime agora, CancellationToken cancellationToken);

    Task<RestaurantePlataformaDetalhesDto?> ObterRestauranteAsync(Guid id, DateTime agora, CancellationToken cancellationToken);

    Task<ResultadoPaginado<AuditoriaItemDto>> ListarAuditoriaAsync(ListarAuditoriaRequest filtro, CancellationToken cancellationToken);
}
