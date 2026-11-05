using Microsoft.EntityFrameworkCore;
using GestaoRestaurante.Pagamentos.Application.Abstractions.Repositories;
using GestaoRestaurante.Pagamentos.Domain.Entities;

namespace GestaoRestaurante.Pagamentos.Infrastructure.Persistence.Repositories;

public class CobrancaPlanoRepository : ICobrancaPlanoRepository
{
    private readonly AppDbContext _context;

    public CobrancaPlanoRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<CobrancaPlano?> ObterPorSessaoSemFiltroAsync(string idSessaoGateway, CancellationToken cancellationToken) =>
        await _context.CobrancasPlano
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(cobranca => cobranca.IdSessaoGateway == idSessaoGateway, cancellationToken);

    public void Adicionar(CobrancaPlano cobranca) => _context.CobrancasPlano.Add(cobranca);

    public void Atualizar(CobrancaPlano cobranca) => _context.Entry(cobranca).State = EntityState.Modified;
}
