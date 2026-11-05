using Microsoft.EntityFrameworkCore;
using GestaoRestaurante.Identidade.Application.Abstractions.Repositories;
using GestaoRestaurante.Identidade.Domain.Entities;

namespace GestaoRestaurante.Identidade.Tests.Common;

public class FakeConviteRepository : IConviteRepository
{
    private readonly TestDbContext _context;

    public FakeConviteRepository(TestDbContext context)
    {
        _context = context;
    }

    public async Task<Convite?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken) =>
        await _context.Convites.FirstOrDefaultAsync(convite => convite.Id == id, cancellationToken);

    public async Task<Convite?> ObterPorIdSemFiltroAsync(Guid id, CancellationToken cancellationToken) =>
        await _context.Convites.IgnoreQueryFilters().FirstOrDefaultAsync(convite => convite.Id == id, cancellationToken);

    public async Task<Convite?> ObterPorHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        await _context.Convites
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(convite => convite.TokenHash == tokenHash, cancellationToken);

    public async Task<IReadOnlyList<Convite>> ListarAsync(CancellationToken cancellationToken) =>
        await _context.Convites
            .OrderByDescending(convite => convite.CriadoEm)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Convite>> ListarPendentesPorEmailAsync(string email, DateTime agora, CancellationToken cancellationToken) =>
        await _context.Convites
            .Where(convite => convite.Email == email
                && convite.AceitoEm == null
                && convite.CanceladoEm == null
                && convite.ExpiraEm > agora)
            .ToListAsync(cancellationToken);

    public async Task<int> ContarPendentesAsync(DateTime agora, string? emailIgnorado, CancellationToken cancellationToken)
    {
        var query = _context.Convites
            .Where(convite => convite.AceitoEm == null
                && convite.CanceladoEm == null
                && convite.ExpiraEm > agora)
            .AsQueryable();

        if (emailIgnorado is not null)
            query = query.Where(convite => convite.Email != emailIgnorado);

        return await query.CountAsync(cancellationToken);
    }

    public void Adicionar(Convite convite) => _context.Convites.Add(convite);

    public void Atualizar(Convite convite) => _context.Entry(convite).State = EntityState.Modified;
}
