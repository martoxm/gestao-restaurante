using Microsoft.EntityFrameworkCore;
using RestauranteEstoque.Application.Abstractions.Repositories;
using RestauranteEstoque.Domain.Entities;
using RestauranteEstoque.Domain.Enums;

namespace RestauranteEstoque.Infrastructure.Persistence.Repositories;

public class MovimentacaoEstoqueRepository : IMovimentacaoEstoqueRepository
{
    private readonly AppDbContext _context;

    public MovimentacaoEstoqueRepository(AppDbContext context)
    {
        _context = context;
    }

    public void Adicionar(MovimentacaoEstoque movimentacao) => _context.MovimentacoesEstoque.Add(movimentacao);

    public async Task<bool> ExisteParaProdutoAsync(Guid produtoId, CancellationToken cancellationToken)
    {
        return await _context.MovimentacoesEstoque.AnyAsync(
            movimentacao => movimentacao.ProdutoId == produtoId,
            cancellationToken);
    }

    public async Task<(IReadOnlyList<MovimentacaoEstoque> Itens, int TotalItens)> ListarPaginadoAsync(
        int pagina,
        int tamanhoPagina,
        Guid? produtoId,
        TipoMovimentacaoEstoque? tipo,
        DateTime? dataInicio,
        DateTime? dataFim,
        CancellationToken cancellationToken)
    {
        var query = _context.MovimentacoesEstoque
            .Include(movimentacao => movimentacao.Produto)
            .AsQueryable();

        if (produtoId.HasValue)
            query = query.Where(movimentacao => movimentacao.ProdutoId == produtoId.Value);

        if (tipo.HasValue)
            query = query.Where(movimentacao => movimentacao.Tipo == tipo.Value);

        if (dataInicio.HasValue)
            query = query.Where(movimentacao => movimentacao.DataMovimentacao >= dataInicio.Value);

        if (dataFim.HasValue)
        {
            var diaSeguinte = dataFim.Value.Date.AddDays(1);
            query = query.Where(movimentacao => movimentacao.DataMovimentacao < diaSeguinte);
        }

        var totalItens = await query.CountAsync(cancellationToken);

        var itens = await query
            .OrderByDescending(movimentacao => movimentacao.DataMovimentacao)
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .ToListAsync(cancellationToken);

        return (itens, totalItens);
    }
}
