namespace GestaoRestaurante.Pagamentos.Application.Abstractions.Services;

public interface IApplicationDbContext
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
