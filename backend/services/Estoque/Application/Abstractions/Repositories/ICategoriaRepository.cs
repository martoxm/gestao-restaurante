using GestaoRestaurante.Contracts.Common;
using GestaoRestaurante.Domain.Entities;

namespace GestaoRestaurante.Application.Abstractions.Repositories;

public interface ICategoriaRepository
{
    Task<Categoria?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken);

    Task<(IReadOnlyList<(Categoria Categoria, int QuantidadeProdutos)> Itens, int TotalItens)> ListarPaginadoAsync(
        int pagina,
        int tamanhoPagina,
        string? nome,
        DirecaoOrdenacao direcaoOrdenacao,
        CancellationToken cancellationToken);

    void Adicionar(Categoria categoria);

    void Atualizar(Categoria categoria);

    Task<Categoria?> ObterExcluidoPorIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Categoria>> ListarExcluidosAsync(CancellationToken cancellationToken);
}
