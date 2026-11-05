using GestaoRestaurante.Identidade.Application.Abstractions.Repositories;
using GestaoRestaurante.Identidade.Domain.Entities;

namespace GestaoRestaurante.Identidade.Infrastructure.Persistence.Repositories;

public class AuditoriaPlataformaRepository : IAuditoriaPlataformaRepository
{
    private readonly AppDbContext _context;

    public AuditoriaPlataformaRepository(AppDbContext context)
    {
        _context = context;
    }

    public void Adicionar(AuditoriaPlataforma auditoria) => _context.AuditoriasPlataforma.Add(auditoria);
}
