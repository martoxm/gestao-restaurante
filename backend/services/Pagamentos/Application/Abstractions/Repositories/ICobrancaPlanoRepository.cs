using GestaoRestaurante.Pagamentos.Domain.Entities;

namespace GestaoRestaurante.Pagamentos.Application.Abstractions.Repositories;

public interface ICobrancaPlanoRepository
{
    Task<CobrancaPlano?> ObterPorSessaoSemFiltroAsync(string idSessaoGateway, CancellationToken cancellationToken);

    void Adicionar(CobrancaPlano cobranca);
    void Atualizar(CobrancaPlano cobranca);
}
