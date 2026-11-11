using GestaoRestaurante.Contracts.Common;
using GestaoRestaurante.Domain.Entities;

namespace GestaoRestaurante.Application.Abstractions.Repositories;

public interface IFornecedorRepository
{
    Task<Fornecedor?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken);

    Task<(IReadOnlyList<Fornecedor> Itens, int TotalItens)> ListarPaginadoAsync(
        int pagina,
        int tamanhoPagina,
        string? nome,
        DirecaoOrdenacao direcaoOrdenacao,
        CancellationToken cancellationToken);

    void Adicionar(Fornecedor fornecedor);

    void Atualizar(Fornecedor fornecedor);

    Task<Fornecedor?> ObterExcluidoPorIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Fornecedor>> ListarExcluidosAsync(CancellationToken cancellationToken);
}
