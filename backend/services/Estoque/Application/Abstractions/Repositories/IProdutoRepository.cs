using GestaoRestaurante.Contracts.Common;
using GestaoRestaurante.Contracts.Produtos;
using GestaoRestaurante.Domain.Entities;

namespace GestaoRestaurante.Application.Abstractions.Repositories;

public interface IProdutoRepository
{
    Task<Produto?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken);

    Task<(IReadOnlyList<Produto> Itens, int TotalItens)> ListarPaginadoAsync(
        int pagina,
        int tamanhoPagina,
        string? nome,
        Guid? categoriaId,
        OrdenacaoProduto ordenarPor,
        DirecaoOrdenacao direcaoOrdenacao,
        CancellationToken cancellationToken);

    void Adicionar(Produto produto);

    void Atualizar(Produto produto);

    Task<Produto?> ObterExcluidoPorIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Produto>> ListarExcluidosAsync(CancellationToken cancellationToken);

    Task<bool> ExisteParaCategoriaAsync(Guid categoriaId, CancellationToken cancellationToken);

    Task<int> ContarPorCategoriaAsync(Guid categoriaId, CancellationToken cancellationToken);
}
