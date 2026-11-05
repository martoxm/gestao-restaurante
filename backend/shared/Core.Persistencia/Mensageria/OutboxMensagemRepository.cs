using Microsoft.EntityFrameworkCore;
using GestaoRestaurante.Core.Mensageria;

namespace GestaoRestaurante.Core.Persistencia.Mensageria;

public class OutboxMensagemRepository<TContext> : IOutboxMensagemRepository
    where TContext : DbContext
{
    private readonly TContext _context;

    public OutboxMensagemRepository(TContext context)
    {
        _context = context;
    }

    public void Adicionar(OutboxMensagem mensagem) => _context.Set<OutboxMensagem>().Add(mensagem);

    public void Atualizar(OutboxMensagem mensagem) => _context.Set<OutboxMensagem>().Update(mensagem);

    public async Task<IReadOnlyList<OutboxMensagem>> ListarPendentesAsync(int quantidade, DateTime agora, CancellationToken cancellationToken)
    {
        return await _context.Set<OutboxMensagem>()
            .Where(mensagem => mensagem.ProcessadoEm == null && mensagem.ProximaTentativaEm <= agora)
            .OrderBy(mensagem => mensagem.CriadoEm)
            .Take(quantidade)
            .ToListAsync(cancellationToken);
    }
}
