using GestaoRestaurante.Identidade.Domain.Entities;

namespace GestaoRestaurante.Identidade.Application.Abstractions.Repositories;

public interface IConviteRepository
{
    Task<Convite?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Convite?> ObterPorIdSemFiltroAsync(Guid id, CancellationToken cancellationToken);

    Task<Convite?> ObterPorHashAsync(string tokenHash, CancellationToken cancellationToken);

    Task<IReadOnlyList<Convite>> ListarAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<Convite>> ListarPendentesPorEmailAsync(string email, DateTime agora, CancellationToken cancellationToken);

    Task<int> ContarPendentesAsync(DateTime agora, string? emailIgnorado, CancellationToken cancellationToken);

    void Adicionar(Convite convite);

    void Atualizar(Convite convite);
}
