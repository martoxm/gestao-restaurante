using Microsoft.EntityFrameworkCore;
using GestaoRestaurante.Vendas.Application.Abstractions.Repositories;
using GestaoRestaurante.Vendas.Domain.Entities;

namespace GestaoRestaurante.Vendas.Infrastructure.Persistence.Repositories;

public class ProdutoCatalogoRepository : IProdutoCatalogoRepository
{
    private readonly AppDbContext _context;

    public ProdutoCatalogoRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<ProdutoCatalogo?> ObterPorIdAsync(Guid produtoId, CancellationToken cancellationToken) =>
        _context.ProdutosCatalogo.FirstOrDefaultAsync(produto => produto.Id == produtoId, cancellationToken);

    public void Adicionar(ProdutoCatalogo produto) => _context.ProdutosCatalogo.Add(produto);

    public void Atualizar(ProdutoCatalogo produto) => _context.Entry(produto).State = EntityState.Modified;
}
