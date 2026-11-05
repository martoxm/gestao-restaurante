using GestaoRestaurante.Vendas.Contracts.Common;
using GestaoRestaurante.Vendas.Domain.Entities;

namespace GestaoRestaurante.Vendas.Application.Abstractions.Repositories;

public interface IVendaRepository
{
    Task<Venda?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken);

    Task<(IReadOnlyList<Venda> Itens, int TotalItens)> ListarPaginadoAsync(
        int pagina,
        int tamanhoPagina,
        DirecaoOrdenacao direcaoOrdenacao,
        CancellationToken cancellationToken);

    Task<int> ObterProximoNumeroAsync(CancellationToken cancellationToken);

    void Adicionar(Venda venda);

    void Atualizar(Venda venda);
}
