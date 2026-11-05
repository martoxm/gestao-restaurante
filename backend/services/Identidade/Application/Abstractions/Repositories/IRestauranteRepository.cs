using GestaoRestaurante.Identidade.Contracts.Common;
using GestaoRestaurante.Identidade.Domain.Entities;

namespace GestaoRestaurante.Identidade.Application.Abstractions.Repositories;

public interface IRestauranteRepository
{
    Task<Restaurante?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken);

    Task<(IReadOnlyList<Restaurante> Itens, int TotalItens)> ListarPaginadoAsync(
        int pagina,
        int tamanhoPagina,
        string? nome,
        DirecaoOrdenacao direcaoOrdenacao,
        CancellationToken cancellationToken);

    Task<Restaurante?> ObterPorIdSemFiltroAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Restaurante>> ListarTestesTerminandoAsync(DateTime agora, DateTime ate, CancellationToken cancellationToken);

    void Adicionar(Restaurante restaurante);

    void Atualizar(Restaurante restaurante);
}
