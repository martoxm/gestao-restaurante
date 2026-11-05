using GestaoRestaurante.Identidade.Application.Abstractions.Repositories;
using GestaoRestaurante.Identidade.Domain.Entities;

namespace GestaoRestaurante.Identidade.Tests.Common;

public class FakeAuditoriaPlataformaRepository : IAuditoriaPlataformaRepository
{
    private readonly TestDbContext _context;

    public FakeAuditoriaPlataformaRepository(TestDbContext context)
    {
        _context = context;
    }

    public void Adicionar(AuditoriaPlataforma auditoria) => _context.AuditoriasPlataforma.Add(auditoria);
}
