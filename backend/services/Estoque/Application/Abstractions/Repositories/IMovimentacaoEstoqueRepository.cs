using GestaoRestaurante.Domain.Entities;
using GestaoRestaurante.Domain.Enums;

namespace GestaoRestaurante.Application.Abstractions.Repositories;

public interface IMovimentacaoEstoqueRepository
{
    void Adicionar(MovimentacaoEstoque movimentacao);

    Task<bool> ExisteParaProdutoAsync(Guid produtoId, CancellationToken cancellationToken);

    Task<(IReadOnlyList<MovimentacaoEstoque> Itens, int TotalItens)> ListarPaginadoAsync(
        int pagina,
        int tamanhoPagina,
        Guid? produtoId,
        TipoMovimentacaoEstoque? tipo,
        DateTime? dataInicio,
        DateTime? dataFim,
        CancellationToken cancellationToken);
}
