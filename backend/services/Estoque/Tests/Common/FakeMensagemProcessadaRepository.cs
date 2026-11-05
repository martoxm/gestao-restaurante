using Microsoft.EntityFrameworkCore;
using GestaoRestaurante.Application.Abstractions.Repositories;
using GestaoRestaurante.Core.Mensageria;

namespace GestaoRestaurante.Tests.Common;

public class FakeMensagemProcessadaRepository : IMensagemProcessadaRepository
{
    private readonly TestDbContext _context;

    public FakeMensagemProcessadaRepository(TestDbContext context)
    {
        _context = context;
    }

    public Task<bool> ExisteAsync(Guid mensagemId, CancellationToken cancellationToken) =>
        _context.MensagensProcessadas.AnyAsync(mensagem => mensagem.Id == mensagemId, cancellationToken);

    public void Adicionar(MensagemProcessada mensagem) => _context.MensagensProcessadas.Add(mensagem);
}
