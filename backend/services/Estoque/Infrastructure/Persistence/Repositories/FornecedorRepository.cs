using GestaoRestaurante.Domain.Common;
using Microsoft.EntityFrameworkCore;
using GestaoRestaurante.Application.Abstractions.Repositories;
using GestaoRestaurante.Contracts.Common;
using GestaoRestaurante.Domain.Entities;

namespace GestaoRestaurante.Infrastructure.Persistence.Repositories;

public class FornecedorRepository : IFornecedorRepository
{
    private readonly AppDbContext _context;

    public FornecedorRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Fornecedor?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.Fornecedores
            .FirstOrDefaultAsync(fornecedor => fornecedor.Id == id, cancellationToken);
    }

    public async Task<(IReadOnlyList<Fornecedor> Itens, int TotalItens)> ListarPaginadoAsync(
        int pagina,
        int tamanhoPagina,
        string? nome,
        DirecaoOrdenacao direcaoOrdenacao,
        CancellationToken cancellationToken)
    {
        var query = _context.Fornecedores.AsQueryable();

        if (!string.IsNullOrWhiteSpace(nome))
            query = query.Where(fornecedor => fornecedor.Nome.Contains(nome));

        var totalItens = await query.CountAsync(cancellationToken);

        query = direcaoOrdenacao == DirecaoOrdenacao.Desc
            ? query.OrderByDescending(fornecedor => fornecedor.Nome)
            : query.OrderBy(fornecedor => fornecedor.Nome);

        var itens = await query
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .ToListAsync(cancellationToken);

        return (itens, totalItens);
    }

    public void Adicionar(Fornecedor fornecedor) => _context.Fornecedores.Add(fornecedor);

    public void Atualizar(Fornecedor fornecedor) => _context.Fornecedores.Update(fornecedor);
    
    public async Task<Fornecedor?> ObterExcluidoPorIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.Fornecedores
            .IgnoreQueryFilters([FiltrosConsulta.NaoExcluido])
            .FirstOrDefaultAsync(fornecedor => fornecedor.Id == id && fornecedor.ExcluidoEm != null, cancellationToken);
    }

    public async Task<IReadOnlyList<Fornecedor>> ListarExcluidosAsync(CancellationToken cancellationToken)
    {
        return await _context.Fornecedores
            .IgnoreQueryFilters([FiltrosConsulta.NaoExcluido])
            .Where(fornecedor => fornecedor.ExcluidoEm != null)
            .OrderByDescending(fornecedor => fornecedor.ExcluidoEm)
            .Take(100)
            .ToListAsync(cancellationToken);
    }
    
}