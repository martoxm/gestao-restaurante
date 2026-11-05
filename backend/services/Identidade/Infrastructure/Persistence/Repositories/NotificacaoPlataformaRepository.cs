using Microsoft.EntityFrameworkCore;
using GestaoRestaurante.Identidade.Application.Abstractions.Repositories;
using GestaoRestaurante.Identidade.Domain.Entities;

namespace GestaoRestaurante.Identidade.Infrastructure.Persistence.Repositories;

public class NotificacaoPlataformaRepository : INotificacaoPlataformaRepository
{
    private readonly AppDbContext _context;

    public NotificacaoPlataformaRepository(AppDbContext context)
    {
        _context = context;
    }

    public void Adicionar(NotificacaoPlataforma notificacao) => _context.NotificacoesPlataforma.Add(notificacao);

    public Task<bool> ExisteChaveAsync(string chave, CancellationToken cancellationToken) =>
        _context.NotificacoesPlataforma.AnyAsync(notificacao => notificacao.Chave == chave, cancellationToken);

    public async Task<(IReadOnlyList<NotificacaoPlataforma> Itens, int Total)> ListarAsync(int pagina, int tamanhoPagina, bool soNaoLidas, CancellationToken cancellationToken)
    {
        var query = _context.NotificacoesPlataforma.AsQueryable();

        if (soNaoLidas)
            query = query.Where(notificacao => notificacao.LidaEm == null);

        var total = await query.CountAsync(cancellationToken);
        var itens = await query
            .OrderByDescending(notificacao => notificacao.OcorridoEm)
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .ToListAsync(cancellationToken);

        return (itens, total);
    }

    public Task<int> ContarNaoLidasAsync(CancellationToken cancellationToken) =>
        _context.NotificacoesPlataforma.CountAsync(notificacao => notificacao.LidaEm == null, cancellationToken);

    public Task<int> MarcarComoLidasAsync(Guid? id, DateTime agora, CancellationToken cancellationToken)
    {
        var query = _context.NotificacoesPlataforma.Where(notificacao => notificacao.LidaEm == null);

        if (id is Guid notificacaoId)
            query = query.Where(notificacao => notificacao.Id == notificacaoId);

        return query.ExecuteUpdateAsync(set => set.SetProperty(notificacao => notificacao.LidaEm, agora), cancellationToken);
    }
}
