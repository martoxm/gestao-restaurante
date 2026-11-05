using GestaoRestaurante.Identidade.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using GestaoRestaurante.Identidade.Application.Abstractions.Repositories;
using GestaoRestaurante.Identidade.Contracts.Common;
using GestaoRestaurante.Identidade.Domain.Entities;

namespace GestaoRestaurante.Identidade.Tests.Common;

public class FakeRestauranteRepository : IRestauranteRepository
{
    private readonly TestDbContext _context;

    public FakeRestauranteRepository(TestDbContext context)
    {
        _context = context;
    }

    public async Task<Restaurante?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.Restaurantes
            .FirstOrDefaultAsync(restaurante => restaurante.Id == id, cancellationToken);
    }

    public async Task<Restaurante?> ObterPorIdSemFiltroAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.Restaurantes
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(restaurante => restaurante.Id == id, cancellationToken);
    }

    public async Task<(IReadOnlyList<Restaurante> Itens, int TotalItens)> ListarPaginadoAsync(
        int pagina,
        int tamanhoPagina,
        string? nome,
        DirecaoOrdenacao direcaoOrdenacao,
        CancellationToken cancellationToken)
    {
        var query = _context.Restaurantes.AsQueryable();

        if (!string.IsNullOrWhiteSpace(nome))
            query = query.Where(restaurante => restaurante.Nome.Contains(nome));

        var totalItens = await query.CountAsync(cancellationToken);

        query = direcaoOrdenacao == DirecaoOrdenacao.Desc
            ? query.OrderByDescending(restaurante => restaurante.Nome)
            : query.OrderBy(restaurante => restaurante.Nome);

        var itens = await query
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .ToListAsync(cancellationToken);

        return (itens, totalItens);
    }

    public void Adicionar(Restaurante restaurante)
    {
        _context.Restaurantes.Add(restaurante);
    }

    public void Atualizar(Restaurante restaurante)
    {
        _context.Restaurantes.Update(restaurante);
    }

    public async Task<IReadOnlyList<Restaurante>> ListarTestesTerminandoAsync(DateTime agora, DateTime ate, CancellationToken cancellationToken)
    {
        return await _context.Restaurantes
            .IgnoreQueryFilters()
            .Where(restaurante => restaurante.Ativo
                && restaurante.Plano == PlanoRestaurante.Teste
                && restaurante.TesteTerminaEm > agora
                && restaurante.TesteTerminaEm <= ate)
            .ToListAsync(cancellationToken);
    }
}
