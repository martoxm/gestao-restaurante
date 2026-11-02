using Microsoft.EntityFrameworkCore;
using RestauranteEstoque.Core.Mensageria;

namespace RestauranteEstoque.Core.Persistencia.Mensageria;

public class MensagemProcessadaRepository<TContext> : IMensagemProcessadaRepository
    where TContext : DbContext
{
    private readonly TContext _context;

    public MensagemProcessadaRepository(TContext context)
    {
        _context = context;
    }

    public Task<bool> ExisteAsync(Guid mensagemId, CancellationToken cancellationToken) =>
        _context.Set<MensagemProcessada>().AnyAsync(mensagem => mensagem.Id == mensagemId, cancellationToken);

    public void Adicionar(MensagemProcessada mensagem) => _context.Set<MensagemProcessada>().Add(mensagem);
}
